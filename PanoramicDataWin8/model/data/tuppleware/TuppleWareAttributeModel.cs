﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.data.sim
{
    [JsonObject(MemberSerialization.OptOut)]
    public class TuppleWareAttributeModel : AttributeModel
    {
        public TuppleWareAttributeModel(int index, string name, string attributeDataType, string attributeVisualizationType)
        {
            _index = index;
            _name = name;
            _attributeDataType = attributeDataType;
            _attributeVisualizationType = attributeVisualizationType;
        }

        private int _index = -1;
        public int Index
        {
            get
            {
                return _index;
            }
        }

        private string _name = "";
        public override string Name
        {
            get
            {
                return _name;
            }
        }

        private string _attributeVisualizationType = "";
        public override string AttributeVisualizationType
        {
            get
            {
                return _attributeVisualizationType;
            }
        }

        private string _attributeDataType = "";
        public override string AttributeDataType
        {
            get
            {
                return _attributeDataType;
            }
        }
    }
}