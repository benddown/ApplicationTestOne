using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Used to access SDK context
using Microsoft.Xrm.Sdk;
// Used to query a record from the database
using Microsoft.Xrm.Sdk.Query;

// testing tek computer
    
namespace ApplicationTestOne.Plugins
{
    public class SetApplicationNumber : IPlugin
    {
        // implementing the Iplugin interface create the execute method
        // which has an IServiceProvider argument that is used to access
        // the organasational service and tracing service
        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                //check the entity in the context ie from the serviceProvider
                IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

                //check if context is the target entity: the context input parameters contain a key "target"
                //which should be "entity" to reference the entity used when registering the plugin

                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    // create a tracing servive for logs
                    ITracingService trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                    // create organisation service to run operations
                    IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

                    // a service is run fro a factory so we now set up the service
                    IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                    // check the registration details to ensure they are correct ie
                    // check message is create
                    // execution event is pre-operation that is stage = 20
                    if (context.MessageName.ToLower() != "create" && context.Stage != 20)
                    {
                        return;
                    }

                    //get any entity in dataveerse by schema name
                    Entity etAutoNumberConfig = new Entity("cr652_applicationautonumberconfig");

                    //get entity in context
                    Entity targetEntity = context.InputParameters["Target"] as Entity;

                    StringBuilder atApplicationNumber = new StringBuilder();
                    string prefix, suffix, separator, day, month, year, currentNumber;
                    DateTime today = DateTime.Now;
                    day = today.Day.ToString("00");
                    month = today.Month.ToString("00");
                    year = today.Year.ToString();

                    //query existing application number config
                    QueryExpression qeAutoNumberConfig = new QueryExpression()
                    {
                        EntityName = "cr652_applicationautonumberconfig",
                        ColumnSet = new ColumnSet("cr652_currentnumber", "cr652_name", "cr652_prefix", "cr652_separator", "cr652_suffix")
                    };

                    // retrieve records
                    EntityCollection ecAutoNumberConfig = service.RetrieveMultiple(qeAutoNumberConfig);

                    // check and ensure some record are returned
                    if (ecAutoNumberConfig.Entities.Count == 0)
                    {
                        return;
                    }

                    // loop over the returned records
                    foreach (Entity entity in ecAutoNumberConfig.Entities)
                    {
                        if (entity.Attributes["cr652_name"].ToString().ToLower() == "india")
                        {
                            prefix = entity.GetAttributeValue<string>("cr652_prefix");
                            suffix = entity.GetAttributeValue<string>("cr652_suffix");
                            separator = entity.GetAttributeValue<string>("cr652_separator");
                            currentNumber = entity.GetAttributeValue<string>("cr652_currentnumber");
                            int tempCurrentNumber = int.Parse(currentNumber);
                            tempCurrentNumber++;

                            //set current number to have 6 characters
                            currentNumber = tempCurrentNumber.ToString("000000");

                            // update the current number field for the entity
                            etAutoNumberConfig.Id = entity.Id;
                            etAutoNumberConfig["cr652_currentnumber"] = tempCurrentNumber.ToString();
                            service.Update(etAutoNumberConfig);

                            //construct application number
                            atApplicationNumber.Append(separator + prefix + separator + year + month + day + separator + suffix + separator + currentNumber);
                            break;
                        }
                    }

                    //update application number
                    targetEntity["cr652_applicationnumber"] = atApplicationNumber.ToString();
                    //service.Update(targetEntity);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
            
        }
    }
}
