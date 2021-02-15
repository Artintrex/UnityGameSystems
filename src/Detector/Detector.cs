using System;
using System.Collections.Generic;
using UnityEngine;

namespace Units.AIUtility
{
    [Flags]
    public enum Range : uint
    {
        None        = 0,
        Short  = 1 << 0,
        Mid    = 1 << 1,
        Long   = 1 << 2,
    }

    public class Detector : MonoBehaviour
    {
        [Header("Detected Objects")]
        public List<Unit> hordeInShort;
        public List<Unit> hordeInMid;
        public List<Unit> hordeInLong;

        public List<Unit> defendersInShort;
        public List<Unit> defendersInMid;
        public List<Unit> defendersInLong;

        public float longRangeDistance, midRangeDistance, shortRangeDistance;
        public float visibilityMultiplier = 1;
        public LayerMask layerMask;
        
        private new Transform transform;
        private float longRangeSqr, midRangeSqr, shortRangeSqr;
        
        public bool GetUnit(Team team, Range range, out Unit unit)
        {
            if ((team & Team.Horde) == Team.Horde)
            {
                if ((range & Range.Short) == Range.Short)
                {
                    if (hordeInShort.Count > 0)
                    {
                        unit = hordeInShort[0];
                        return true;
                    }
                }
            
                if ((range & Range.Mid) == Range.Mid)
                {
                    if (hordeInMid.Count > 0)
                    {
                        unit = hordeInMid[0];
                        return true;
                    }
                }

                if ((range & Range.Long) != Range.Long)
                {
                    unit = null;
                    return false;
                }
                if (hordeInLong.Count > 0)
                {
                    unit = hordeInLong[0];
                    return true;
                }
            }else if ((team & Team.Player) == Team.Player)
            {
                if ((range & Range.Short) == Range.Short)
                {
                    if (defendersInShort.Count > 0)
                    {
                        unit = defendersInShort[0];
                        return true;
                    }
                }
            
                if ((range & Range.Mid) == Range.Mid)
                {
                    if (defendersInMid.Count > 0)
                    {
                        unit = defendersInMid[0];
                        return true;
                    }
                }

                if ((range & Range.Long) != Range.Long)
                {
                    unit = null;
                    return false;
                }

                if (defendersInLong.Count > 0)
                {
                    unit = defendersInLong[0];
                    return false;
                }
            }
            unit = null;
            return false;
        }

        public bool IsUnitInRange(Unit unit, Range range)
        {
            if (unit.info.team == Team.Player)
            {
                if ((range & Range.Long) == Range.Long)
                {
                    foreach (Unit go in defendersInLong)
                    {
                        if (go == unit)
                        {
                            return true;
                        }
                    }
                }
                if ((range & Range.Mid) == Range.Mid)
                {
                    foreach (Unit go in defendersInMid)
                    {
                        if (go == unit)
                        {
                            return true;
                        }
                    }
                }
                if ((range & Range.Short) == Range.Short)
                {
                    foreach (Unit go in defendersInShort)
                    {
                        if (go == unit)
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                if ((range & Range.Long) == Range.Long)
                {
                    foreach (Unit go in hordeInLong)
                    {
                        if (go == unit)
                        {
                            return true;
                        }
                    }
                }
                if ((range & Range.Mid) == Range.Mid)
                {
                    foreach (Unit go in hordeInMid)
                    {
                        if (go == unit)
                        {
                            return true;
                        }
                    }
                }
                if ((range & Range.Short) == Range.Short)
                {
                    foreach (Unit go in hordeInShort)
                    {
                        if (go == unit)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void Start()
        {
            transform = gameObject.transform;
            collidersInRange = new Collider[64];

            SquareViewDistance();
            oldVisibilityMultiplier = visibilityMultiplier;
        }

        private void OnEnable()
        {
            StageManager.instance.rangeCalculationPollingList.Add(this);
        }

        private void OnDisable()
        {
            hordeInShort.Clear();
            hordeInMid.Clear();
            hordeInLong.Clear();
            defendersInShort.Clear();
            defendersInMid.Clear();
            defendersInLong.Clear();
            
            StageManager.instance.rangeCalculationPollingList.Remove(this);
        }

        private void SquareViewDistance()
        {
            float distance = longRangeDistance * visibilityMultiplier;
            longRangeSqr = distance * distance;
            
            distance = midRangeDistance * visibilityMultiplier;
            midRangeSqr = distance * distance;

            distance = shortRangeDistance * visibilityMultiplier; 
            shortRangeSqr = distance * distance;
        }

        private Collider[] collidersInRange;
        private float oldVisibilityMultiplier;
        public void CalculateRanges()
        {
            Vector3 position = transform.position;
            
            //Get colliders in long range
            var size = Physics.OverlapBoxNonAlloc(position, new Vector3(longRangeDistance,longRangeDistance,longRangeDistance), collidersInRange, Quaternion.identity, layerMask);
            hordeInShort.Clear();
            hordeInMid.Clear();
            hordeInLong.Clear();
            defendersInShort.Clear();
            defendersInMid.Clear();
            defendersInLong.Clear();

            //If there is a change in view distance multiplier recalculate square values
            if(Mathf.Abs(oldVisibilityMultiplier - visibilityMultiplier) > float.Epsilon) SquareViewDistance();
            
            //Go through all colliders and try to get their Unit component.
            //Then sort into respective container depending on distance.
            for (int i = 0; i < size; ++i)
            {
                Unit unit = collidersInRange[i].gameObject.GetComponent<Unit>();
                
                if (!unit) continue;
                
                Vector3 vector = position - unit.transform.position;
                
                float distance = vector.sqrMagnitude;

                if (unit.info.team == Team.Player)
                {
                    if (distance <= shortRangeSqr)
                    {
                        defendersInShort.Add(unit);
                    }else if (distance <= midRangeSqr)
                    {
                        defendersInMid.Add(unit);
                    }else if (distance <= longRangeSqr)
                    {
                        defendersInLong.Add(unit);
                    }
                }else if (unit.info.team == Team.Horde)
                {
                    if (distance <= shortRangeSqr)
                    {
                        hordeInShort.Add(unit);
                    }else if (distance <= midRangeSqr)
                    {
                        hordeInMid.Add(unit);
                    }else if (distance <= longRangeSqr)
                    {
                        hordeInLong.Add(unit);
                    }
                }
            }
            //Enlarge size for the next scan if space is not enough
            if (size > collidersInRange.Length)
            {
                collidersInRange = new Collider[collidersInRange.Length * 2];
            }
        }

        private void OnDrawGizmosSelected()
        {
            Color color = Color.green;
            color.a = 0.1f;
            Gizmos.color = color;
            Vector3 position = gameObject.transform.position;
            Gizmos.DrawSphere(position, longRangeDistance * visibilityMultiplier);
            color.a = 0.2f;
            Gizmos.color = color;
            Gizmos.DrawSphere(position, midRangeDistance * visibilityMultiplier);
            color.a = 0.3f;
            Gizmos.color = color;
            Gizmos.DrawSphere(position, shortRangeDistance * visibilityMultiplier);
        }
    }
}