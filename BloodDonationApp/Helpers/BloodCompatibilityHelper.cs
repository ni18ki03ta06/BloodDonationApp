using System.Collections.Generic;

namespace BloodDonationApp.Helpers
{
    public static class BloodCompatibilityHelper
    {
        public static List<string> GetCompatibleBloodTypes(string requestedType)
        {
            var compatible = new List<string>();
            if (string.IsNullOrEmpty(requestedType)) return compatible;

            switch (requestedType.ToUpper().Trim())
            {
                case "O-":
                    compatible.Add("O-");
                    break;
                case "O+":
                    compatible.AddRange(new[] { "O-", "O+" });
                    break;
                case "A-":
                    compatible.AddRange(new[] { "O-", "A-" });
                    break;
                case "A+":
                    compatible.AddRange(new[] { "O-", "O+", "A-", "A+" });
                    break;
                case "B-":
                    compatible.AddRange(new[] { "O-", "B-" });
                    break;
                case "B+":
                    compatible.AddRange(new[] { "O-", "O+", "B-", "B+" });
                    break;
                case "AB-":
                    compatible.AddRange(new[] { "O-", "A-", "B-", "AB-" });
                    break;
                case "AB+":
                    compatible.AddRange(new[] { "O-", "O+", "A-", "A+", "B-", "B+", "AB-", "AB+" });
                    break;
            }
            return compatible;
        }
    }
}
