public enum DeploymentStep
{
    AddProject,
    BuildingDockerImage,
    BuildingServer,
    BuildingClient,
    ShowStarted,
    ZippingServer,
    ZippingClient,
    GetServerUploadURL,
    UploadServer,
    GetClientUploadURL,
    UploadClient,
    BuildImage,
    PingRegions,
    Deploying,
    ShowFinished
}