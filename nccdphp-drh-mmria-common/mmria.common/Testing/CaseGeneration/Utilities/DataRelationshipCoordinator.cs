using System;
using System.Collections.Generic;

namespace mmria.common.Testing.CaseGeneration.Utilities
{
    /// <summary>
    /// Coordinates data generation across related fields to ensure logical consistency
    /// </summary>
    public class DataRelationshipCoordinator
    {
        private readonly Dictionary<string, object?> _generatedValues;
        private readonly Random _random;

        public DataRelationshipCoordinator(Random random)
        {
            _random = random;
            _generatedValues = new Dictionary<string, object?>();
        }

        /// <summary>
        /// Store a generated value for later reference
        /// </summary>
        public void StoreValue(string key, object? value)
        {
            _generatedValues[key] = value;
        }

        /// <summary>
        /// Get a previously generated value
        /// </summary>
        public object? GetValue(string key)
        {
            return _generatedValues.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Generate date of death after date of birth
        /// </summary>
        public DateTime? GenerateDateOfDeath(DateTime? dateOfBirth)
        {
            if (!dateOfBirth.HasValue)
            {
                // Default to recent past if no birth date
                return DateTime.Now.AddDays(-_random.Next(1, 730)); // 0-2 years ago
            }

            // Death must be after birth
            var minAge = TimeSpan.FromDays(0); // Can be same day (stillbirth)
            var maxAge = TimeSpan.FromDays(365 * 50); // Max 50 years
            
            var ageAtDeath = TimeSpan.FromDays(_random.Next(
                (int)minAge.TotalDays,
                (int)maxAge.TotalDays
            ));

            var dateOfDeath = dateOfBirth.Value.Add(ageAtDeath);
            
            // Ensure not in future
            if (dateOfDeath > DateTime.Now)
            {
                dateOfDeath = DateTime.Now.AddDays(-_random.Next(1, 365));
            }

            return dateOfDeath;
        }

        /// <summary>
        /// Generate LMP (Last Menstrual Period) before date of birth
        /// </summary>
        public DateTime? GenerateLMP(DateTime? dateOfBirth)
        {
            if (!dateOfBirth.HasValue)
            {
                // Default to ~9 months ago
                return DateTime.Now.AddDays(-280 + _random.Next(-30, 30));
            }

            // LMP should be ~280 days (40 weeks) before birth
            // With some variation for premature/overdue
            var gestationDays = _random.Next(224, 301); // 32-43 weeks
            return dateOfBirth.Value.AddDays(-gestationDays);
        }

        /// <summary>
        /// Generate prenatal visit date within pregnancy period
        /// </summary>
        public DateTime? GeneratePrenatalVisit(DateTime? lmp, DateTime? dateOfBirth, int visitNumber)
        {
            DateTime startDate;
            DateTime endDate;

            if (lmp.HasValue && dateOfBirth.HasValue)
            {
                startDate = lmp.Value;
                endDate = dateOfBirth.Value;
            }
            else if (dateOfBirth.HasValue)
            {
                startDate = dateOfBirth.Value.AddDays(-280);
                endDate = dateOfBirth.Value;
            }
            else
            {
                // Default pregnancy period
                startDate = DateTime.Now.AddDays(-280);
                endDate = DateTime.Now;
            }

            // Distribute visits across pregnancy
            // Typical: 1st trimester, 2nd trimester, 3rd trimester visits
            var pregnancyDuration = (endDate - startDate).TotalDays;
            var visitWindow = pregnancyDuration / Math.Max(visitNumber + 1, 4);
            
            var visitDay = startDate.AddDays(visitWindow * visitNumber + _random.Next(-14, 14));
            
            // Ensure within pregnancy period
            if (visitDay < startDate) visitDay = startDate.AddDays(7);
            if (visitDay > endDate) visitDay = endDate.AddDays(-7);
            
            return visitDay;
        }

        /// <summary>
        /// Generate admission date before discharge date
        /// </summary>
        public DateTime? GenerateAdmissionDate(DateTime? dischargeDate)
        {
            if (!dischargeDate.HasValue)
            {
                // Default to recent past
                return DateTime.Now.AddDays(-_random.Next(1, 30));
            }

            // Typical hospital stays: 1-14 days
            var stayDuration = _random.Next(1, 15);
            return dischargeDate.Value.AddDays(-stayDuration);
        }

        /// <summary>
        /// Generate discharge date after admission date
        /// </summary>
        public DateTime? GenerateDischargeDate(DateTime? admissionDate)
        {
            if (!admissionDate.HasValue)
            {
                // Default to recent past
                return DateTime.Now.AddDays(-_random.Next(1, 30));
            }

            // Typical hospital stays: 1-14 days
            var stayDuration = _random.Next(1, 15);
            var dischargeDate = admissionDate.Value.AddDays(stayDuration);
            
            // Ensure not in future
            if (dischargeDate > DateTime.Now)
            {
                dischargeDate = DateTime.Now.AddDays(-_random.Next(0, 3));
            }

            return dischargeDate;
        }

        /// <summary>
        /// Generate child age based on mother's age
        /// </summary>
        public int? GenerateChildAge(int? motherAge)
        {
            if (!motherAge.HasValue)
            {
                return _random.Next(0, 2); // 0-2 years for child
            }

            // Child should be younger than mother (obviously)
            // For maternal mortality, child is typically newborn or young
            return _random.Next(0, Math.Min(3, motherAge.Value / 2));
        }

        /// <summary>
        /// Generate gestational age (weeks) with realistic variation
        /// </summary>
        public int? GenerateGestationalAge(bool isPremature)
        {
            if (isPremature)
            {
                // Premature: 24-36 weeks
                return _random.Next(24, 37);
            }
            else
            {
                // Full term: 37-42 weeks
                return _random.Next(37, 43);
            }
        }

        /// <summary>
        /// Clear stored values (e.g., between cases)
        /// </summary>
        public void Clear()
        {
            _generatedValues.Clear();
        }
    }
}

