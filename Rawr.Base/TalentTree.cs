﻿using System;
using System.Collections.Generic;

namespace Rawr
{
    [Serializable]
    public class TalentTree
    {
        private SerializableDictionary<int, string> _treeNames = new SerializableDictionary<int, string>();

        [System.Xml.Serialization.XmlElement("Trees")]
        public SerializableDictionary<string, List<TalentItem>> _trees = new SerializableDictionary<string, List<TalentItem>>();

        [System.Xml.Serialization.XmlElement("Region")]
        public Character.CharacterRegion _region;

        [System.Xml.Serialization.XmlElement("Realm")]
        public string _realm;

        [System.Xml.Serialization.XmlElement("Name")]
        public string _name;

        [System.Xml.Serialization.XmlElement("Class")]
        public Character.CharacterClass _class = Character.CharacterClass.Druid;






        #region Properties

        [System.Xml.Serialization.XmlIgnore]
        public SerializableDictionary<string, List<TalentItem>> Trees
        {
            get { return _trees; }
        }



        [System.Xml.Serialization.XmlIgnore]
        public Character.CharacterRegion Region
        {
            get { return _region; }
            set { _region = value; }
        }

        [System.Xml.Serialization.XmlIgnore]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [System.Xml.Serialization.XmlIgnore]
        public Character.CharacterClass Class
        {
            get { return _class; }
            set { _class = value; }
        }

        [System.Xml.Serialization.XmlIgnore]
        public string Realm
        {
            get { return _realm; }
            set { _realm = value; }
        }


        #endregion


        public TalentTree()
        {
        }


        public TalentTree(Character character)
        {
            SetCharacter(character);
        }

        public void SetCharacter(Character character)
        {
            _region = character.Region;
            _realm = character.Realm;
            _name = character.Name;
            _class = character.Class;
            buildTreeFramework();
        }

        public TalentItem GetTalent(string TalentName)
        {
            TalentItem ti = null;
            foreach (string tree in _trees.Keys)
            {
                ti = _trees[tree].Find(delegate (TalentItem talent) { return talent.Name.Replace(" ", "").ToUpper() == TalentName.Replace(" ", "").ToUpper(); });
                if (ti != null) return ti;
            }
            return new TalentItem();
        }

        public override string ToString()
        {
            string talentString = "(";
            string labelString = "";
            foreach (string name in _trees.Keys)
            {
                labelString += name + "/";
                int talentPoints = 0;
                foreach (TalentItem ti in _trees[name])
                {
                    talentPoints += ti.PointsInvested;
                }
                talentString += talentPoints + "/";
            }
            labelString = labelString.Substring(0, labelString.Length - 1);
            talentString = talentString.Substring(0, talentString.Length - 1);
            talentString += ")";
            return labelString + " " + talentString;

        }


        private bool fullyQualified()
        {
            return (!string.IsNullOrEmpty(_realm) && !string.IsNullOrEmpty(_name));
        }

        private void buildTreeFramework()
        {
            if (fullyQualified())
            {
                WebRequestWrapper wrw = new WebRequestWrapper();
                string talentTree = wrw.DownloadClassTalentTree(_class);
                string talentCode = wrw.DownloadCharacterTalentTree(_name, _region, _realm).SelectSingleNode("page/characterInfo/talentTab/talentTree").Attributes["value"].Value;
                populateTrees(talentTree, talentCode);

            }
        }


        private void populateTrees(string talentTree, string talentCode)
        {
            string[] treeDesc = talentTree.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            List<string> treeNames = new List<string>();
            int row = 0;

            //determine tree names
            while (_trees.Keys.Count < 3)
            {
                if (treeDesc[row].Contains("tree[i] ="))
                {
                    treeNames.Add(treeDesc[row].Split('"')[1]);
                    _trees.Add(treeDesc[row].Split('"')[1], new List<TalentItem>());
                }
                row++;
            }

            int treeNum = 0;
            //build talent description lists for each tree
            for (row = 0; row <= treeDesc.GetLength(0); row++)
            {
                string currRow = treeDesc[row];
                if (currRow.StartsWith("talent["))
                {
                    _trees[treeNames[treeNum]].Add(new TalentItem(currRow, treeNames[treeNum]));
                    if (!treeDesc[row + 1].StartsWith("talent[")) treeNum++;
                }
                if (treeNum == 3) break;
            }

            int currTal = 0;
            //parse talent string into talent lists
            for (int n = 0; n < treeNames.Count; n++)
            {
                List<TalentItem> currTree = _trees[treeNames[n]];
                for (int treeIndex = 0; treeIndex < currTree.Count; treeIndex++)
                {
                    currTree[treeIndex].PointsInvested = Int32.Parse(talentCode.Substring(currTal, 1));
                    currTal++;
                }

            }

        }

        //private void GetTalentString()
        //{
        //    string talentUrl = String.Format(_talentsBase, _region ==  Character.CharacterRegion.US ? "www" : "eu", _realm, _name);
        //    WebRequestWrapper wrw = new WebRequestWrapper();
        //    XmlDocument talents = wrw.DownloadXml(talentUrl);
        //}
    }
}
