using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.ExceptionServices;
using UnityEditor;

namespace Unordinal.Editor
{
    public partial class GuiHosting
    {
        #region Fields

        private float stepInterpolation = 0.0f;

        // Progress for the different steps in the deployment
        // The numbers represent the progress when the step is finished.
        private Dictionary<DeploymentStep, float> stepToProgressDictionary = new Dictionary<DeploymentStep, float>()
        {
            {DeploymentStep.ShowStarted, 0.0f},
            {DeploymentStep.ZippingServer, 0.15f}, // 0 -> 15

            // Client stuff (only some times)
            {DeploymentStep.ZippingClient, 0.25f}, // 15 -> 25

            {DeploymentStep.GetServerUploadURL, 0.25f},
            {DeploymentStep.UploadServer, 0.45f}, // 25 -> 45

            // Client stuff (only some times)
            {DeploymentStep.GetClientUploadURL, 0.45f},
            {DeploymentStep.UploadClient, 0.60f}, // 45 -> 60
            
            {DeploymentStep.BuildImage, 0.75f}, // 60 -> 75
            {DeploymentStep.PingRegions, 0.75f},

            // After deploying finished the progress bar will be at 95%.
            // This is to prevent progress bar from getting stuck at 99% if we miss-calculate the .
            // Instead the user gets stuck at 94% (which is not as frustrating as 99%)
            {DeploymentStep.Deploying, 0.95f}, // 75 -> 95

            {DeploymentStep.ShowFinished, 1.0f}
        };

        // Estimated deployment times
        private Dictionary<DeploymentStep, float> estimatedTimesDictionary = new Dictionary<DeploymentStep, float>()
        {
            { DeploymentStep.ZippingClient, 120000.0f},     // 2 min
            { DeploymentStep.ZippingServer, 1200000.0f},    // 2 min
            { DeploymentStep.GetClientUploadURL, 2000.0f},
            { DeploymentStep.GetServerUploadURL, 2000.0f},
            { DeploymentStep.UploadClient,  240000.0f},     // 4 min
            { DeploymentStep.UploadServer,  240000.0f},     // 4 min
            { DeploymentStep.BuildImage,    120000.0f},     // 2 min
            { DeploymentStep.PingRegions,   2000.0f},
            { DeploymentStep.Deploying,     600000.0f},    // 10 min (this is to try to avoid user getting stuck on last percentage)
            { DeploymentStep.ShowFinished,  500.0f},
        };
        
        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        #endregion

        /// <summary>
        /// Use this method to smoothen the progress bar progress. 
        /// Instead of jumping from e.g. 10% directly to 30%, the progress can be smoothen to show values in between.
        /// 
        /// Note that this method will never go past the next step, in other words: when interpolating between 10% to 30% the value will
        /// never go past the 30% value.
        /// </summary>
        /// <param name="func">The function we are waiting for to complete.</param>
        /// <param name="step">The current step in the deployment.</param>
        private async Task InterpolateProgressBarBetweenSteps(Func<Task> func, DeploymentStep step)
        {
            // Reset the interpolation since a new step has been started.
            stepInterpolation = 0.0f;

            sw.Restart();

#if DEBUG
            if(addDebugButtons)
            {
                Debug.Log(step.ToString() + ": Started");
            }
#endif

            var task = func();
            
            // Loop to see when task is finished.
            while (!task.IsCompleted)
            {
                int delayTime = 100;
                await Task.Delay(delayTime);
                var estimatedTime = estimatedTimesDictionary[step];
                estimatedTime *= 1.2f; // Add 20% estimate since games evolve over time and take longer to deploy.
                stepInterpolation += ((float)delayTime) / estimatedTime;
                float finishedProgress = stepToProgressDictionary[step-1];
                float nextProgress = stepToProgressDictionary[step];
                Progress = ((float)finishedProgress + Mathf.Min(stepInterpolation, 0.99f) * (nextProgress - finishedProgress));
                if(nonAwaitedTaskFailed)
                {
                    throw new Exception(failMessage);
                }
            }
    
            if(deploymentFlow.IsCancellationRequested)
            {
#if DEBUG
                if(addDebugButtons)
                {
                    Debug.Log("Step canceled");
                }
#endif
                return; 
            }

            if (task.Exception != null)
            {
                // An exception occured in the given function.

                var exInfo = ExceptionDispatchInfo.Capture(task.Exception);
                if(task.Exception.InnerExceptions.Count > 0)
                {
                    // Inner exception has proven to be more usefull a lot of the times.
                    exInfo = ExceptionDispatchInfo.Capture(task.Exception.InnerExceptions[0]);
                }
                
                exInfo.Throw();
            }

            sw.Stop();

            // Task is finished
            Progress = stepToProgressDictionary[step];

            if (step == DeploymentStep.ShowFinished)
            {
                return; // Don't change the estimated time of this fixed time.
            }

            // Update the estimated times, make it influenced by both by both previous results and the newest elapsed time.
            // The value is however most influenced by the most recent elapsed time.
            //                               30% from old                            70% from new
            estimatedTimesDictionary[step] = 0.3f * estimatedTimesDictionary[step] + 0.7f * sw.ElapsedMilliseconds;
            SaveEstimatedTimes();
        }

        #region EditorPrefs

        private void LoadEstimatedTimes()
        {
            if (EditorPrefs.HasKey(UnordinalKeys.zipTimeKey))
                estimatedTimesDictionary[DeploymentStep.ZippingServer] = EditorPrefs.GetFloat(UnordinalKeys.zipTimeKey);

            if (EditorPrefs.HasKey(UnordinalKeys.uploadClientUrlTimeKey))
                estimatedTimesDictionary[DeploymentStep.GetClientUploadURL] = EditorPrefs.GetFloat(UnordinalKeys.uploadServerUrlTimeKey);
            if (EditorPrefs.HasKey(UnordinalKeys.uploadClientTimeKey))
                estimatedTimesDictionary[DeploymentStep.UploadClient] = EditorPrefs.GetFloat(UnordinalKeys.uploadServerTimeKey);

            if (EditorPrefs.HasKey(UnordinalKeys.uploadServerUrlTimeKey))
                estimatedTimesDictionary[DeploymentStep.GetServerUploadURL] = EditorPrefs.GetFloat(UnordinalKeys.uploadServerUrlTimeKey);
            if (EditorPrefs.HasKey(UnordinalKeys.uploadServerTimeKey))
                estimatedTimesDictionary[DeploymentStep.UploadServer] = EditorPrefs.GetFloat(UnordinalKeys.uploadServerTimeKey);

            if (EditorPrefs.HasKey(UnordinalKeys.buildingImageTimeKey))
                estimatedTimesDictionary[DeploymentStep.BuildImage] = EditorPrefs.GetFloat(UnordinalKeys.buildingImageTimeKey);

            if (EditorPrefs.HasKey(UnordinalKeys.pingRegionsTimeKey))
                estimatedTimesDictionary[DeploymentStep.PingRegions] = EditorPrefs.GetFloat(UnordinalKeys.pingRegionsTimeKey);

            // Commented out to ensure default value is used. 
            //if (EditorPrefs.HasKey(UnordinalEditorPrefs.deployingTimeKey))
            //    estimatedTimesDictionary[DeploymentStep.Deploying] = EditorPrefs.GetFloat(UnordinalEditorPrefs.deployingTimeKey);
        }

        private void SaveEstimatedTimes()
        {
            EditorPrefs.SetFloat(UnordinalKeys.zipTimeKey, estimatedTimesDictionary[DeploymentStep.ZippingServer]);
            EditorPrefs.SetFloat(UnordinalKeys.uploadClientUrlTimeKey, estimatedTimesDictionary[DeploymentStep.GetClientUploadURL]);
            EditorPrefs.SetFloat(UnordinalKeys.uploadClientTimeKey, estimatedTimesDictionary[DeploymentStep.UploadClient]);
            EditorPrefs.SetFloat(UnordinalKeys.uploadServerUrlTimeKey, estimatedTimesDictionary[DeploymentStep.GetServerUploadURL]);
            EditorPrefs.SetFloat(UnordinalKeys.uploadServerTimeKey, estimatedTimesDictionary[DeploymentStep.UploadServer]);
            EditorPrefs.SetFloat(UnordinalKeys.buildingImageTimeKey, estimatedTimesDictionary[DeploymentStep.BuildImage]);
            EditorPrefs.SetFloat(UnordinalKeys.pingRegionsTimeKey, estimatedTimesDictionary[DeploymentStep.PingRegions]);
            EditorPrefs.SetFloat(UnordinalKeys.deployingTimeKey, estimatedTimesDictionary[DeploymentStep.Deploying]);
        }

        #endregion

        #region Experimental

        // This method might be used in the future.
        private void LoadPercentages()
        {
            if (EditorPrefs.HasKey(UnordinalKeys.zipPercentageKey))
                stepToProgressDictionary[DeploymentStep.ZippingServer] = EditorPrefs.GetFloat(UnordinalKeys.zipPercentageKey);
            if (EditorPrefs.HasKey(UnordinalKeys.dockerPercentageKey))
                stepToProgressDictionary[DeploymentStep.BuildingDockerImage] = EditorPrefs.GetFloat(UnordinalKeys.dockerPercentageKey);
            if (EditorPrefs.HasKey(UnordinalKeys.uploadClientUrlPercentageKey))
                stepToProgressDictionary[DeploymentStep.GetClientUploadURL] = EditorPrefs.GetFloat(UnordinalKeys.uploadClientUrlPercentageKey);
            if (EditorPrefs.HasKey(UnordinalKeys.uploadClientPercentageKey))
                stepToProgressDictionary[DeploymentStep.UploadClient] = EditorPrefs.GetFloat(UnordinalKeys.uploadClientPercentageKey);
            if (EditorPrefs.HasKey(UnordinalKeys.uploadServerUrlPercentageKey))
                stepToProgressDictionary[DeploymentStep.GetServerUploadURL] = EditorPrefs.GetFloat(UnordinalKeys.uploadServerUrlPercentageKey);
            if (EditorPrefs.HasKey(UnordinalKeys.uploadServerPercentageKey))
                stepToProgressDictionary[DeploymentStep.UploadServer] = EditorPrefs.GetFloat(UnordinalKeys.uploadServerPercentageKey);
            if (EditorPrefs.HasKey(UnordinalKeys.buildingImagePercentageKey))
                stepToProgressDictionary[DeploymentStep.BuildImage] = EditorPrefs.GetFloat(UnordinalKeys.buildingImagePercentageKey);
            if (EditorPrefs.HasKey(UnordinalKeys.pingRegionsPercentageKey))
                stepToProgressDictionary[DeploymentStep.PingRegions] = EditorPrefs.GetFloat(UnordinalKeys.pingRegionsPercentageKey);
            if (EditorPrefs.HasKey(UnordinalKeys.deployingPercentageKey))
                stepToProgressDictionary[DeploymentStep.Deploying] = EditorPrefs.GetFloat(UnordinalKeys.deployingPercentageKey);
        }

        // This method might be used in the future.
        /// Calculates the percentage spent on this step in the last deployment.
        /// These values are then stored in order to be used during the next deployment, in order
        /// to make the ProgressBar move smooth during the whole deployment.
        private void CalculateAndSavePercentages()
        {
            // Get totalTime of Deployment
            var totalTime = 0.0f;
            foreach(var kvp in estimatedTimesDictionary)
            {
                if (kvp.Key == DeploymentStep.ShowStarted || kvp.Key == DeploymentStep.ShowFinished)
                {
                    continue;
                }

                totalTime += kvp.Value;
            }

            var stepPercentage = 0.0f;
            var deploymentIsFinishedAtPercentage = stepToProgressDictionary[DeploymentStep.Deploying];

            // Zipping
            stepPercentage += estimatedTimesDictionary[DeploymentStep.ZippingServer] / totalTime;
            EditorPrefs.SetFloat(UnordinalKeys.zipPercentageKey, stepPercentage * deploymentIsFinishedAtPercentage);

            // Get upload client URL
            stepPercentage += estimatedTimesDictionary[DeploymentStep.GetClientUploadURL] / totalTime;
            EditorPrefs.SetFloat(UnordinalKeys.uploadClientUrlPercentageKey, stepPercentage * deploymentIsFinishedAtPercentage);

            // Uploading client
            stepPercentage += estimatedTimesDictionary[DeploymentStep.UploadClient] / totalTime;
            EditorPrefs.SetFloat(UnordinalKeys.uploadClientPercentageKey, stepPercentage * deploymentIsFinishedAtPercentage);

            // Get upload server URL
            stepPercentage += estimatedTimesDictionary[DeploymentStep.GetServerUploadURL] / totalTime;
            EditorPrefs.SetFloat(UnordinalKeys.uploadServerUrlPercentageKey, stepPercentage * deploymentIsFinishedAtPercentage);

            // Uploading server
            stepPercentage += estimatedTimesDictionary[DeploymentStep.UploadServer] / totalTime;
            EditorPrefs.SetFloat(UnordinalKeys.uploadServerPercentageKey, stepPercentage * deploymentIsFinishedAtPercentage);

            // Build image
            stepPercentage += estimatedTimesDictionary[DeploymentStep.BuildImage] / totalTime;
            EditorPrefs.SetFloat(UnordinalKeys.buildingImagePercentageKey, stepPercentage * deploymentIsFinishedAtPercentage);

            // Ping regions
            stepPercentage += estimatedTimesDictionary[DeploymentStep.PingRegions] / totalTime;
            EditorPrefs.SetFloat(UnordinalKeys.pingRegionsPercentageKey, stepPercentage * deploymentIsFinishedAtPercentage);

            // Deploy
            stepPercentage += estimatedTimesDictionary[DeploymentStep.Deploying] / totalTime;
            EditorPrefs.SetFloat(UnordinalKeys.deployingPercentageKey, stepPercentage * deploymentIsFinishedAtPercentage);
        }

        #endregion
    }
}
