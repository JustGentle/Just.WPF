namespace Just.Base.Views
{
    public interface IChildView : IDependency
    {
        void ReadSettings(string[] args);
        void WriteSettings();
    }
}
