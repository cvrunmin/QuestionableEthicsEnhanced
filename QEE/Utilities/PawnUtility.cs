﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace QEthics
{
    [StaticConstructorOnStartup]
    public static class PawnUtility
    {
        public static Building_Bed FindAvailNonMedicalBed(this Pawn sleeper, Pawn traveler)
        {
            bool sleeperWillBePrisoner = sleeper.IsPrisoner;

            if (sleeper.InBed())
            {
                QEEMod.TryLog("Sleeper is in bed");
                return sleeper.CurrentBed();
            }

            QEEMod.TryLog("Count of all BedDefs: " + RestUtility.AllBedDefBestToWorst.Count);
            for (int i = 0; i < RestUtility.AllBedDefBestToWorst.Count; i++)
            {
                ThingDef thingDef = RestUtility.AllBedDefBestToWorst[i];
                if (RestUtility.CanUseBedEver(sleeper, thingDef))
                {
                    Building_Bed building_Bed = (Building_Bed)GenClosest.ClosestThingReachable(sleeper.Position, sleeper.Map, 
                        ThingRequest.ForDef(thingDef), PathEndMode.OnCell, TraverseParms.For(traveler, Danger.Deadly, 
                        TraverseMode.ByPawn, false), 9999f, delegate (Thing b)
                    {
                        return IsValidNonMedicalBed(b, sleeper, traveler, sleeperWillBePrisoner, false);
                    }, null, 0, -1, false, RegionType.Set_Passable, false);

                    if (building_Bed != null)
                    {
                        QEEMod.TryLog("QE_BedSelected".Translate(building_Bed.def.defName));
                        return building_Bed;
                    }
                }
            }

            return null;
        }

        //this is a modified version of vanilla's RestUtility.IsValidBedFor() that excludes medical beds
        public static bool IsValidNonMedicalBed(Thing bedThing, Pawn sleeper, Pawn traveler, bool sleeperWillBePrisoner, 
            bool checkSocialProperness, bool ignoreOtherReservations = false)
        {
            Building_Bed building_Bed = bedThing as Building_Bed;
            if (building_Bed == null)
            {
                QEEMod.TryLog("QE_BedInvalid".Translate(bedThing.def.defName));
                return false;
            }
            LocalTargetInfo target = building_Bed;
            PathEndMode peMode = PathEndMode.OnCell;
            Danger maxDanger = Danger.Some;
            int sleepingSlotsCount = building_Bed.SleepingSlotsCount;

            if (building_Bed.Medical)
            {
                QEEMod.TryLog("QE_BedIsMedical".Translate(building_Bed.def.defName));
                return false;
            }
            else if (!traveler.CanReserveAndReach(target, peMode, maxDanger, sleepingSlotsCount, -1, null, ignoreOtherReservations))
            {
                QEEMod.TryLog("QE_BedUnreachableBySurgeon".Translate(building_Bed.def.defName, traveler.Named("SURGEON")));
                return false;
            }
            else if (building_Bed.Position.GetDangerFor(sleeper, sleeper.Map) > maxDanger)
            {
                QEEMod.TryLog("QE_BedTooDangerous".Translate(building_Bed.def.defName, traveler.Named("PATIENT")));
                return false;
            }
            else if (traveler.HasReserved<JobDriver_TakeToBed>(building_Bed, sleeper))
            {
                QEEMod.TryLog("QE_BedSurgeonAlreadyReserved".Translate(building_Bed.def.defName, traveler.Named("SURGEON")));
                return false;
            }
            else if (!building_Bed.AnyUnoccupiedSleepingSlot && (!sleeper.InBed() || sleeper.CurrentBed() != building_Bed) && !building_Bed.AssignedPawns.Contains(sleeper))
            {
                QEEMod.TryLog("QE_BedOccupied".Translate(building_Bed.def.defName));
                return false;
            }
            else if (building_Bed.IsForbidden(traveler))
            {
                QEEMod.TryLog("QE_BedForbidden".Translate(building_Bed.def.defName));
                return false;
            }
            else if (checkSocialProperness && !building_Bed.IsSociallyProper(sleeper, sleeperWillBePrisoner))
            {
                QEEMod.TryLog("QE_BedFailsSocialChecks".Translate(building_Bed.def.defName));
                return false;
            }
            else if (building_Bed.IsBurning())
            {
                QEEMod.TryLog("QE_BedIsBurning".Translate(building_Bed.def.defName));
                return false;
            }
            else if (sleeperWillBePrisoner)
            {
                if (!building_Bed.ForPrisoners)
                {
                    QEEMod.TryLog("QE_BedImproperForPrisoner".Translate(building_Bed.def.defName, traveler.Named("PATIENT")));
                    return false;
                }
                if (!building_Bed.Position.IsInPrisonCell(building_Bed.Map))
                {
                    QEEMod.TryLog("QE_BedNotInPrisonCell".Translate(building_Bed.def.defName, traveler.Named("PATIENT")));
                    return false;
                }
            }
            else
            {
                if (building_Bed.Faction != traveler.Faction && (traveler.HostFaction == null || building_Bed.Faction != traveler.HostFaction))
                {
                    QEEMod.TryLog("QE_BedImproperFaction".Translate(building_Bed.def.defName, traveler.Named("SURGEON")));
                    return false;
                }
                if (building_Bed.ForPrisoners)
                {
                    QEEMod.TryLog("QE_PrisonerBedForNonPrisoner".Translate(building_Bed.def.defName, traveler.Named("PATIENT")));
                    return false;
                }
            }
            //fail if bed is owned
            if (building_Bed.owners.Any() && !building_Bed.owners.Contains(sleeper) && !building_Bed.AnyUnownedSleepingSlot)
            {
                QEEMod.TryLog("QE_BedHasAnotherOwner".Translate(building_Bed.def.defName));
                return false;
            }
            return true;
        }
    }
}
