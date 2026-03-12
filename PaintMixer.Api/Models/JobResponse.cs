namespace PaintMixer.Api
{
    public record JobSubmittedResponse(int JobCode);
    public record JobStatusResponse(int JobCode, string Status);
    public record MessageResponse(string Message);
}
