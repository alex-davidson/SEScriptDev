namespace IngameScript
{
    interface IAccumulator<T> where T : struct
    {
        T Accumulate(T record);
    }
}
