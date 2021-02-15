using System.Collections.Generic;
using Units;
using Units.AIUtility;
using UnityEngine;

namespace System
{
    public class StageManager : MonoBehaviour
    {
        public static StageManager instance { get; private set; }

        public GameObject theHeart;
        public Vector3 theHeartPos;
        
        public List<Detector> rangeCalculationPollingList;

        //Id dictionary for loaded units
        private Dictionary<uint, Unit> units;
    
        private void Awake()
        {
            instance = this;
            
            units = new Dictionary<uint, Unit>();
            
            rangeCalculationPollingList = new List<Detector>();
        }

        private void Start()
        {
            theHeartPos = theHeart.transform.position;
        }

        private void Update()
        {
            UpdateDetectors();
        }

        //Should be called during loading a save to restore previous id
        public void RegisterUnitIDs(uint id, Unit unit)
        {
            units[id] = unit;
        }

        //id 0 is null
        private uint idCounter = 1;
        //Assigns a new id to given object
        public uint GetNewId(Unit unit)
        {
            while (idCounter < uint.MaxValue)
            {
                if (!units.ContainsKey(idCounter))
                {
                    units.Add(idCounter, unit);
                    return idCounter;
                }
                else idCounter++;
            }
            return 0;
        }

        public Unit GetUnit(uint id)
        {
            return units[id];
        }

        private int counter;
        private void UpdateDetectors()
        {
            if (counter >= rangeCalculationPollingList.Count) counter = 0;
            
            int targetCount = counter + 20;
            
            if (targetCount > rangeCalculationPollingList.Count) targetCount = rangeCalculationPollingList.Count;
            
            for (int i = counter; i < targetCount; ++i)
            {
                rangeCalculationPollingList[i].CalculateRanges();
            }
            counter = targetCount;
        }
    }
}