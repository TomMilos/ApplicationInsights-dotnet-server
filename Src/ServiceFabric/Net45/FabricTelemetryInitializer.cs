﻿using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace Microsoft.ApplicationInsights.ServiceFabric
{
    /// <summary>
    /// Telemetry initializer for service fabric. Adds service fabric specific context to outgoing telemetry.
    /// </summary>
    public class FabricTelemetryInitializer : ITelemetryInitializer
    {
        private const string ServiceContextKeyName = "AI.SF.ServiceContext";

        private Dictionary<string, string> contextCollection;

        /// <summary>
        /// There are a few ways the context could be provided. This property makes it easy for the rest of the implemenatation to ignore all those cases. 
        /// </summary>
        private Dictionary<string, string> ApplicableServiceContext
        {
            get
            {
                if (this.contextCollection != null)
                {
                    return this.contextCollection;
                }

                return CallContext.LogicalGetData(ServiceContextKeyName) as Dictionary<string, string>;
                //if (fromCallContext != null)
                //{
                //    return fromCallContext;
                //}
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FabricTelemetryInitializer"/> class.
        /// </summary>
        /// <param name="context">a service context object.</param>
        public FabricTelemetryInitializer(ServiceContext context)
        {
            this.contextCollection = GetContextContractDictionaryFromServiceContext(context);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FabricTelemetryInitializer"/> class.
        /// </summary>
        public FabricTelemetryInitializer()
        {}

        /// <summary>
        /// This static method is a helper method that anyone can invoke to set the call context.
        /// This provides a way for the user to add a single line of code at the entry point and get collected telemetry augmented with service fabric specific fields.
        /// </summary>
        /// <param name="context">A service context object.</param>
        public static void SetServiceCallContext(ServiceContext context)
        {
            // The call initializes TelemetryConfiguration that will create and Intialize modules.
            TelemetryConfiguration configuration = TelemetryConfiguration.Active;

            CallContext.LogicalSetData(ServiceContextKeyName, GetContextContractDictionaryFromServiceContext(context));
        }

        /// <summary>
        /// Adds service fabric context fields on the given telemetry object.
        /// </summary>
        /// <param name="telemetry">The telemetry item being sent through the AI sdk.</param>
        public void Initialize(ITelemetry telemetry)
        {
            try
            {
                if (this.ApplicableServiceContext != null)
                {
                    foreach (var field in this.ApplicableServiceContext)
                    {
                        if (!telemetry.Context.Properties.ContainsKey(field.Key))
                        {
                            telemetry.Context.Properties.Add(field.Key, field.Value);
                        }
                    }

                    if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName) && this.ApplicableServiceContext.ContainsKey(KnownContextFieldNames.ServiceName))
                    {
                        telemetry.Context.Cloud.RoleName = this.ApplicableServiceContext[KnownContextFieldNames.ServiceName];
                    }
                    if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleInstance))
                    {
                        if (this.ApplicableServiceContext.ContainsKey(KnownContextFieldNames.InstanceId))
                        {
                            telemetry.Context.Cloud.RoleInstance = this.ApplicableServiceContext[KnownContextFieldNames.InstanceId];
                        }
                        else if (this.ApplicableServiceContext.ContainsKey(KnownContextFieldNames.ReplicaId))
                        {
                            telemetry.Context.Cloud.RoleInstance = this.ApplicableServiceContext[KnownContextFieldNames.ReplicaId];
                        }
                    }
                }

                // Fallback to environment variables for setting role / instance names. We will rely on these environment variables exclusively for container lift and shift scenarios for now.
                // And for reliable services, when service context is neither provided directly nor through call context
                if(string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName))
                {
                    telemetry.Context.Cloud.RoleName = Environment.GetEnvironmentVariable(KnownEnvironmentVariableName.ServicePackageName);
                }
                
                if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleInstance))
                {
                    telemetry.Context.Cloud.RoleInstance = Environment.GetEnvironmentVariable(KnownEnvironmentVariableName.ServicePackageActivatonId) ?? Environment.GetEnvironmentVariable(KnownEnvironmentVariableName.ServicePackageInstanceId);
                }

                if (!telemetry.Context.Properties.ContainsKey(KnownContextFieldNames.NodeName))
                {
                    string nodeName = Environment.GetEnvironmentVariable(KnownEnvironmentVariableName.NodeName);

                    if (!string.IsNullOrEmpty(nodeName))
                    {
                        telemetry.Context.Properties.Add(KnownContextFieldNames.NodeName, nodeName);
                    }
                }
            }
            catch
            {
                // Something went wrong trying to set these extra properties. We shouldn't fail though.
            }
        }

        /// <summary>
        /// Converts the context object to the loose dictionary based contract this initializer depends on for data.
        /// </summary>
        /// <param name="context">An object of type <see cref="ServiceContext" />.</param>
        /// <returns>A dictionary that encapsulates the given context.</returns>
        private static Dictionary<string,string> GetContextContractDictionaryFromServiceContext(ServiceContext context)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            if (context != null)
            {
                result.Add(KnownContextFieldNames.ServiceName, context.ServiceName.ToString());
                result.Add(KnownContextFieldNames.ServiceTypeName, context.ServiceTypeName);
                result.Add(KnownContextFieldNames.PartitionId, context.PartitionId.ToString());
                result.Add(KnownContextFieldNames.ApplicationName, context.CodePackageActivationContext.ApplicationName);
                result.Add(KnownContextFieldNames.ApplicationTypeName, context.CodePackageActivationContext.ApplicationTypeName);
                result.Add(KnownContextFieldNames.NodeName, context.NodeContext.NodeName);
                if (context is StatelessServiceContext)
                {
                    result.Add(KnownContextFieldNames.InstanceId, context.ReplicaOrInstanceId.ToString());
                }

                if (context is StatefulServiceContext)
                {
                    result.Add(KnownContextFieldNames.ReplicaId, context.ReplicaOrInstanceId.ToString());
                }
            }

            return result;
        }

        private class KnownContextFieldNames
        {
            public const string ServiceName = "ServiceName";
            public const string ServiceTypeName = "ServiceTypeName";
            public const string PartitionId = "PartitionId";
            public const string ApplicationName = "ApplicationName";
            public const string ApplicationTypeName = "ApplicationTypeName";
            public const string NodeName = "NodeName";
            public const string InstanceId = "InstanceId";
            public const string ReplicaId = "ReplicaId";
        }

        private class KnownEnvironmentVariableName
        {
            public const string ServicePackageName = "Fabric_ServicePackageName";
            public const string ServicePackageInstanceId = "Fabric_ServicePackageInstanceId";
            public const string ServicePackageActivatonId = "Fabric_ServicePackageActivationId";
            public const string NodeName = "Fabric_NodeName";
        }
    }
}

