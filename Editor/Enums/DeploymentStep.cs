enum DeploymentStep
{
    ShowStarted,
    BuildingServer,
    ZippingServer,
    BuildingDockerImage,
    GetUploadURL,
    UploadFile,
    BuildImage,
    PingRegions,
    Deploying,
    ShowFinished
}