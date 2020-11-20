﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Rawr.WCFService
{
    // NOTE: If you change the interface name "IService1" here, you must also update the reference to "IService1" in Web.config.
    [ServiceContract]
    public interface IRawrService
    {

        [OperationContract]
        string[] GetSupportedModels();

        [OperationContract]
        Dictionary<string, string> GetCharacterDisplayCalculationValues(string character, string model);
    }


    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    [DataContract]
    public class CompositeType
    {
        bool boolValue = true;
        string stringValue = "Hello ";

        [DataMember]
        public bool BoolValue
        {
            get { return boolValue; }
            set { boolValue = value; }
        }

        [DataMember]
        public string StringValue
        {
            get { return stringValue; }
            set { stringValue = value; }
        }
    }
}
