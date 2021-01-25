﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Key;
using mzmr.Items;
using Randomizer.ItemRules;

namespace mzmr.ItemRules
{
    public class ItemRule : IEquatable<ItemRule>
    {
        public Items.ItemType ItemType { get; set; }
        public RuleTypes.RuleType RuleType { get; set; }
        public int Value { get; set; }

        public List<ItemRuleBase> ToLogicRules()
        {
            var rules = new List<ItemRuleBase>();
            var keyId = KeyManager.GetKeyFromName(ItemType.LogicName())?.Id ?? Guid.Empty;
            if(keyId == Guid.Empty)
            {
                return rules;
            }

            try
            {
                if (RuleType == RuleTypes.RuleType.PrioritizedAfterSearchDepth)
                {
                    rules.Add(new ItemRulePrioritizedAfterDepth() { ItemId = keyId, SearchDepth = Value });
                }

                if (RuleType == RuleTypes.RuleType.RestrictedBeforeSearchDepth)
                {
                    rules.Add(new ItemRuleRestrictedBeforeDepth() { ItemId = keyId, SearchDepth = Value });
                }

                if (RuleType == RuleTypes.RuleType.InLocation)
                {
                    var location = Location.GetLocation(Value);
                    rules.Add(new ItemRuleInLocation() { ItemId = keyId, LocationIdentifier = location.LogicName });
                }

                if (RuleType == RuleTypes.RuleType.NotInLocation)
                {
                    var location = Location.GetLocation(Value);
                    rules.Add(new ItemRuleNotInLocation() { ItemId = keyId, LocationIdentifier = location.LogicName });
                }

                if (RuleType == RuleTypes.RuleType.InArea)
                {
                    var locations = Location.GetLocations().Where(location => location.Area == Value);
                    rules.AddRange(locations.Select(location => new ItemRuleInLocation() { ItemId = keyId, LocationIdentifier = location.LogicName }));
                }

                if (RuleType == RuleTypes.RuleType.NotInArea)
                {
                    var locations = Location.GetLocations().Where(location => location.Area == Value);
                    rules.AddRange(locations.Select(location => new ItemRuleNotInLocation() { ItemId = keyId, LocationIdentifier = location.LogicName }));
                }
            }
            catch { }

            return rules;
        }

        public bool Equals(ItemRule other)
        {
            // If parameter is null, return false.
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            // Optimization for a common success case.
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (this.GetType() != other.GetType())
            {
                return false;
            }

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            if ((ItemType != other.ItemType) || (RuleType != other.RuleType) || !(Value != other.Value))
            {
                return false;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (int)ItemType * 0x1000000 + (int)RuleType * 0x0010000 + Value;
        }

        public override bool Equals(object other)
        {
            // Check for null on left side.
            if (Object.ReferenceEquals(this, null))
            {
                if (Object.ReferenceEquals(other, null))
                {
                    // null == null = true.
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return this.Equals(other);
        }

        public static bool operator ==(ItemRule lhs, ItemRule rhs)
        {
            // Check for null on left side.
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null))
                {
                    // null == null = true.
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ItemRule lhs, ItemRule rhs)
        {
            return !(lhs == rhs);
        }
    }
}