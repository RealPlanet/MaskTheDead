using BepInEx.Configuration;

namespace MaskTheDead
{
    public class Configuration
    {
        public float PossesionChance => configPossessionChance.Value;
        public int RetryPossesionTime => configRetryPossesionTime.Value;
        public int PossessionDelayTime => configPossessionDelayTime.Value;
        public int MaskPossessionRange => configMaskPossessionRange.Value;
        public int RetryPossesionMinTime => 10000;

        private ConfigEntry<float> configPossessionChance;
        private ConfigEntry<int> configPossessionDelayTime;
        private ConfigEntry<int> configRetryPossesionTime;
        private ConfigEntry<int> configMaskPossessionRange;
        private ConfigFile File;

        public Configuration(ConfigFile config)
        {
            File = config;
            configPossessionChance = File.Bind("General",
                "PossessionChance",
                1f,
                "A number between 0 and 1 (included) representing the percentage of a body being possessed by a nearby mask");

            configRetryPossesionTime = File.Bind("General",
                 "RetryPossesionTime",
                 RetryPossesionMinTime,
                 $"Time in milliseconds after which a mask will attempt to posses a nearby body again, less than {RetryPossesionMinTime} milliseconds is ignored");

            configMaskPossessionRange = File.Bind("General",
                 "MaskPossessionRange",
                 10,
                 $"Range, in units, of the mask repossession trigger. Only bodies close enough will be considered for repossessions!");

            configPossessionDelayTime = File.Bind("General",
                 "PossessionDelayTime",
                 0,
                 $"Time in milliseconds before the body is actually possessed");

        }
    }
}
