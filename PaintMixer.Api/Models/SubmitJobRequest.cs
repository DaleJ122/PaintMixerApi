namespace PaintMixer.Api
{
    public record SubmitJobRequest(
        int Red = 0,
        int Black = 0,
        int White = 0,
        int Yellow = 0,
        int Blue = 0,
        int Green = 0);
}
