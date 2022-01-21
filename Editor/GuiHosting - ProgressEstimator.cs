using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;
using static Unordinal.Hosting.UnordinalApi;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Microsoft.Extensions.Logging;
using Unity;
using System.Threading;
using System.Runtime.ExceptionServices;

namespace Unordinal.Hosting
{
    public partial class GuiHosting
    {
        #region Fields

        private float stepInterpolation = 0.0f;

        // PlayerPrefs keys
        private string zipTimeKey = "estimatedSecondsZipping";
        private string dockerTimeKey = "estimatedSecondsBuildingDockerImage";
        private string uploadUrlTimeKey = "estimatedSecondsGetUploadUrl";
        private string uploadFileTimeKey = "estimatedSecondsUploadFile";
        private string buildingImageTimeKey = "estimatedSecondsBuildingImage";
        private string pingRegionsTimeKey = "estimatedSecondsPingRegions";
        private string deployingTimeKey = "estimatedSecondsDeploying";

        // Progress for the different steps in the deployment
        // The numbers represent the progress when the step is finished.
        private Dictionary<DeploymentStep, float> stepToProgressDictionary = new Dictionary<DeploymentStep, float>()
        {
            //{DeploymentStep.ShowStarted, 0.01f},
            {DeploymentStep.BuildingServer, 0.0f},
            {DeploymentStep.ZippingServer, 0.15f},
            {DeploymentStep.BuildingDockerImage, 0.18f},
            {DeploymentStep.GetUploadURL, 0.21f},
            {DeploymentStep.UploadFile, 0.35f}, // 21  -> 35
            {DeploymentStep.BuildImage, 0.58f}, // 35 -> 58
            {DeploymentStep.PingRegions, 0.60f},

            // After deploying finished the progress bar will be at 95%.
            // This is to prevent progress bar from getting stuck at 99% if we miss-calculate the .
            // Instead the user gets stuck at 94% (which is not as frustrating as 99%)
            {DeploymentStep.Deploying, 0.95f}, // 60 -> 95

            {DeploymentStep.ShowFinished, 1.0f}
        };

        // Estimated deployment times
        private Dictionary<DeploymentStep, float> estimatedTimesDictionary = new Dictionary<DeploymentStep, float>()
        {
            { DeploymentStep.ZippingServer, 11000.0f},
            { DeploymentStep.BuildingDockerImage, 2000.0f},
            { DeploymentStep.GetUploadURL, 2000.0f},
            { DeploymentStep.UploadFile, 30000.0f},
            { DeploymentStep.BuildImage, 1200000.0f},   // 20 min (this is to try to avoid user getting stuck on last percentage)
            { DeploymentStep.PingRegions, 5000.0f},
            { DeploymentStep.Deploying, 1200000.0f},    // 20 min (this is to try to avoid user getting stuck on last percentage)
            { DeploymentStep.ShowFinished, 500.0f},
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
            Debug.Log(step.ToString() + ": Started");
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
                progress = ((float)finishedProgress + Mathf.Min(stepInterpolation, 0.99f) * (nextProgress - finishedProgress));
                OnGUI(); // Refresh UI to ensure latest progress is visible.
            }
    
            if(deploymentFlow.IsCancellationRequested)
            {
#if DEBUG
                Debug.Log("Step canceled");
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
            progress = stepToProgressDictionary[step];
            OnGUI(); // Refresh UI to ensure latest progress is visible.

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

        #region PlayerPrefs

        private void LoadEstimatedTimes()
        {
            if (PlayerPrefs.HasKey(zipTimeKey))
                estimatedTimesDictionary[DeploymentStep.ZippingServer] = PlayerPrefs.GetFloat(zipTimeKey);
            if (PlayerPrefs.HasKey(dockerTimeKey))
                estimatedTimesDictionary[DeploymentStep.BuildingDockerImage] = PlayerPrefs.GetFloat(dockerTimeKey);
            if (PlayerPrefs.HasKey(uploadUrlTimeKey))
                estimatedTimesDictionary[DeploymentStep.GetUploadURL] = PlayerPrefs.GetFloat(uploadUrlTimeKey);
            if (PlayerPrefs.HasKey(uploadFileTimeKey))
                estimatedTimesDictionary[DeploymentStep.UploadFile] = PlayerPrefs.GetFloat(uploadFileTimeKey);
            
            // Commented out to ensure default value is used. 
            //if (PlayerPrefs.HasKey(buildingImageTimeKey))
            //    estimatedTimesDictionary[DeploymentStep.BuildImage] = PlayerPrefs.GetFloat(buildingImageTimeKey);

            if (PlayerPrefs.HasKey(pingRegionsTimeKey))
                estimatedTimesDictionary[DeploymentStep.PingRegions] = PlayerPrefs.GetFloat(pingRegionsTimeKey);

            // Commented out to ensure default value is used. 
            //if (PlayerPrefs.HasKey(deployingTimeKey))
            //    estimatedTimesDictionary[DeploymentStep.Deploying] = PlayerPrefs.GetFloat(deployingTimeKey);
        }

        private void SaveEstimatedTimes()
        {
            PlayerPrefs.SetFloat(zipTimeKey, estimatedTimesDictionary[DeploymentStep.ZippingServer]);
            PlayerPrefs.SetFloat(dockerTimeKey, estimatedTimesDictionary[DeploymentStep.BuildingDockerImage]);
            PlayerPrefs.SetFloat(uploadUrlTimeKey, estimatedTimesDictionary[DeploymentStep.GetUploadURL]);
            PlayerPrefs.SetFloat(uploadFileTimeKey, estimatedTimesDictionary[DeploymentStep.UploadFile]);
            PlayerPrefs.SetFloat(buildingImageTimeKey, estimatedTimesDictionary[DeploymentStep.BuildImage]);
            PlayerPrefs.SetFloat(pingRegionsTimeKey, estimatedTimesDictionary[DeploymentStep.PingRegions]);
            PlayerPrefs.SetFloat(deployingTimeKey, estimatedTimesDictionary[DeploymentStep.Deploying]);
        }

        #endregion

        #region Experimental

        // More PlayerPrefs keys
        private string zipPercentageKey = "percentageZipping";
        private string dockerPercentageKey = "percentageBuildingDockerImage";
        private string uploadUrlPercentageKey = "percentageGetUploadUrl";
        private string uploadFilePercentageKey = "percentageUploadFile";
        private string buildingImagePercentageKey = "percentageBuildingImage";
        private string pingRegionsPercentageKey = "percentagePingRegions";
        private string deployingPercentageKey = "percentageDeploying";

        // This method might be used in the future.
        private void LoadPercentages()
        {
            if (PlayerPrefs.HasKey(zipPercentageKey))
                stepToProgressDictionary[DeploymentStep.ZippingServer] = PlayerPrefs.GetFloat(zipPercentageKey);
            if (PlayerPrefs.HasKey(dockerPercentageKey))
                stepToProgressDictionary[DeploymentStep.BuildingDockerImage] = PlayerPrefs.GetFloat(dockerPercentageKey);
            if (PlayerPrefs.HasKey(uploadUrlPercentageKey))
                stepToProgressDictionary[DeploymentStep.GetUploadURL] = PlayerPrefs.GetFloat(uploadUrlPercentageKey);
            if (PlayerPrefs.HasKey(uploadFilePercentageKey))
                stepToProgressDictionary[DeploymentStep.UploadFile] = PlayerPrefs.GetFloat(uploadFilePercentageKey);
            if (PlayerPrefs.HasKey(buildingImagePercentageKey))
                stepToProgressDictionary[DeploymentStep.BuildImage] = PlayerPrefs.GetFloat(buildingImagePercentageKey);
            if (PlayerPrefs.HasKey(pingRegionsPercentageKey))
                stepToProgressDictionary[DeploymentStep.PingRegions] = PlayerPrefs.GetFloat(pingRegionsPercentageKey);
            if (PlayerPrefs.HasKey(deployingPercentageKey))
                stepToProgressDictionary[DeploymentStep.Deploying] = PlayerPrefs.GetFloat(deployingPercentageKey);
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
            PlayerPrefs.SetFloat(zipPercentageKey, stepPercentage * deploymentIsFinishedAtPercentage);

            // Building docker image
            stepPercentage += estimatedTimesDictionary[DeploymentStep.BuildingDockerImage] / totalTime;
            PlayerPrefs.SetFloat(dockerPercentageKey, stepPercentage * deploymentIsFinishedAtPercentage);

            // Get upload URL
            stepPercentage += estimatedTimesDictionary[DeploymentStep.GetUploadURL] / totalTime;
            PlayerPrefs.SetFloat(uploadUrlPercentageKey, stepPercentage * deploymentIsFinishedAtPercentage);

            // Uploading file
            stepPercentage += estimatedTimesDictionary[DeploymentStep.UploadFile] / totalTime;
            PlayerPrefs.SetFloat(uploadFilePercentageKey, stepPercentage * deploymentIsFinishedAtPercentage);

            // Build image
            stepPercentage += estimatedTimesDictionary[DeploymentStep.BuildImage] / totalTime;
            PlayerPrefs.SetFloat(buildingImagePercentageKey, stepPercentage * deploymentIsFinishedAtPercentage);

            // Ping regions
            stepPercentage += estimatedTimesDictionary[DeploymentStep.PingRegions] / totalTime;
            PlayerPrefs.SetFloat(pingRegionsPercentageKey, stepPercentage * deploymentIsFinishedAtPercentage);

            // Deploy
            stepPercentage += estimatedTimesDictionary[DeploymentStep.Deploying] / totalTime;
            PlayerPrefs.SetFloat(deployingPercentageKey, stepPercentage * deploymentIsFinishedAtPercentage);
        }

        #endregion
    }
}