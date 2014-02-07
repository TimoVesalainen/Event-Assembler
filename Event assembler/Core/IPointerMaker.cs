namespace Nintenlord.Event_Assembler.Core
{
    /// <summary>
    /// Makes pointers out of offsets
    /// </summary>
    public interface IPointerMaker<T>
    {
        /// <summary>
        /// Creates a valid pointer out of offset
        /// </summary>
        /// <param name="offset">Offset to transform</param>
        /// <returns>A pointer</returns>
        T MakePointer(T offset);
        /// <summary>
        /// Makes a offset out of a pointer
        /// </summary>
        /// <param name="pointer">Pointer whose pointed offset to get</param>
        /// <returns>Offset pointed by the pointer</returns>
        T MakeOffset(T pointer);
        /// <summary>
        /// Checks if the pointer is valid
        /// </summary>
        /// <param name="pointer">Pointer to check</param>
        /// <returns>True if pointer is valid, else false</returns>
        bool IsAValidPointer(T pointer);
    }
}
