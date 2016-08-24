﻿namespace Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    internal struct DependencyTelemetryDocument : ITelemetryDocument
    {
        [DataMember(EmitDefaultValue = false)]
        public string Version { get; set; }
        
        [DataMember]
        public DateTimeOffset Timestamp { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Id { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public DateTimeOffset StartTime { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool? Success { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TimeSpan Duration { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ResultCode { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string CommandName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string DependencyTypeName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string DependencyKind { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string DocumentType
        {
            get
            {
                return TelemetryDocumentType.RemoteDependency.ToString();
            }

            private set
            {
            }
        }
    }
}