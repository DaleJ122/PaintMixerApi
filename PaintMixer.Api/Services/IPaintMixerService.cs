namespace PaintMixer.Api
{
    public interface IPaintMixerService
    {
        int SubmitJob(int red, int black, int white, int yellow, int blue, int green);
        int CancelJob(int jobCode);
        int QueryJobState(int jobCode);
    }
}
