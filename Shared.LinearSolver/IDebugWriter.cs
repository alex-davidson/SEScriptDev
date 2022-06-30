namespace Shared.LinearSolver
{
    public interface IDebugWriter
    {
        void Write(string phase, Tableau tableau);
        void Write(string message);
        void WritePivot(Tableau tableau, int row, int column);
    }
}
