namespace InexperiencedDeveloper.Utils
{
    public static class MathUtils
    {
        /// <summary>
        /// Remaps a selected value within a certain range to its associated value within a different range
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="newMin"></param>
        /// <param name="newMax"></param>
        /// <returns></returns>
        public static float Remap(float val, float min, float max, float newMin, float newMax)
        {
            return newMin + (val - min) * (newMax - newMin) / (max - min);
        }

        /// <summary>
        /// Remaps the selected value from 0-1
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static float Remap01(float val, float min, float max)
        {
            return Remap(val, min, max, 0, 1);
        }
    }
}
