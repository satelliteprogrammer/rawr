﻿using System;
using System.Collections.Generic;

namespace Rawr.Tree
{
    public class SpellRotation
    {
        public List<Spell> spells
        {
            get;
            private set;
        }

        public String cycleSpells
        {
            get;
            private set;
        }

        private int numCyclesInRotation = 1;

        public SpellRotation(List<SpellRotation> spellCycles, float maxRotationDuration, int numCyclesInRotation)
        {
            float sum = 0f;
            this.spells = new List<Spell>();
            cycleSpells = "";
            this.maxCycleDuration = maxRotationDuration;
            this.numCyclesInRotation = numCyclesInRotation;

            foreach (SpellRotation sr in spellCycles)
            {
                foreach (Spell s in sr.spells)
                {
                    if (sum + s.CastTime <= maxCycleDuration)
                    {
                        sum += s.CastTime;
                        this.spells.Add(s);
                    }
                    else
                    {
                        throw new OverflowException("Could not add all cycles to the rotation");
                    }
                }

                cycleSpells += sr.cycleSpells;
            }

            this.tightCycleDuration = sum;
            this.currentCycleDuration = this.tightCycleDuration;
        }

        public SpellRotation(List<Spell> spells, float maxCycleDuration)
        {
            float sum = 0f;
            this.spells = new List<Spell>();
            cycleSpells = "";

            foreach (Spell s in spells)
            {
                if (sum + s.CastTime <= maxCycleDuration)
                {
                    sum += s.CastTime;
                    if (cycleSpells.Length != 0)
                        cycleSpells += ", ";
                    cycleSpells += s.Name;
                    this.spells.Add(s);
                }
            }

            this.maxCycleDuration = maxCycleDuration;
            this.tightCycleDuration = sum;

            if (cycleSpells.Length != 0)
                cycleSpells = String.Format("{0:0.0}s: {1}\n", tightCycleDuration, cycleSpells);
        }

        public float currentCycleDuration
        {
            get;
            set;
        }

        public float bestCycleDuration
        {
            get;
            set;
        }

        public float maxCycleDuration
        {
            get;
            private set;
        }

        public int numberOfSpells
        {
            get
            {
                return spells.FindAll(delegate (Spell s)
                {
                    return !(s is Nothing);
                }).Count;
            }
        }

        public float tightCycleDuration
        {
            get;
            private set;
        }

        public float manaPerCycle
        {
            get
            {
                float sum = 0f;
                foreach (Spell s in spells)
                {
                    sum += s.Cost;
                }
                return sum;
            }
        }

        public float healPerCycle
        {
            get
            {
                float sum = 0f;
                foreach (Spell s in spells)
                {
                    // Stacked lifebloom is the only heal expected not to run out
                    // All other heals are expected to run out, providing their maximum possible heal
                    if (s is LifebloomStack)
                        sum += s.PeriodicTick * currentCycleDuration / numCyclesInRotation;
                    else
                        sum += s.AverageTotalHeal;
                }
                return sum;
            }
        }

        public float HPM
        {
            get
            {
                return healPerCycle / manaPerCycle;
            }
        }
    }
}
