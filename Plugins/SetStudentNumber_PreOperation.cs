using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace ApplicationTestOne.Plugins
{
    public class SetStudentNumber_PreOperation : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                // business logic
                IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

                // validate entity
                if(context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    ITracingService trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
                    IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                    IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                    if(context.MessageName.ToLower() != "create" && context.Stage != 20)
                    {
                        return;
                    }

                    //get entity to be created
                    Entity studentRecord = context.InputParameters["Target"] as Entity;

                    // get autonumber record
                    Entity autoNumberRecord = new Entity("cr652_applicationautonumberconfig");

                    QueryExpression qeAutoNumber = new QueryExpression()
                    {
                        EntityName = "cr652_applicationautonumberconfig",
                        ColumnSet = new ColumnSet("cr652_currentnumber", "cr652_name", "cr652_prefix", "cr652_separator", "cr652_suffix")
                    };

                    qeAutoNumber.Criteria.AddCondition("cr652_name", ConditionOperator.Equal, "student auto number");

                    EntityCollection ecAutoNumbers = service.RetrieveMultiple(qeAutoNumber);

                    if(ecAutoNumbers.Entities.Count == 0)
                    {
                        throw new Exception("No auto number record was found!");
                    }

                    autoNumberRecord = ecAutoNumbers.Entities[0];

                    DateTime today = DateTime.Now;
                    string year = today.Year.ToString();
                    string month = today.Month.ToString("00");
                    string day = today.Day.ToString("00");
                    string prefix = autoNumberRecord["cr652_prefix"].ToString();
                    string suffix = autoNumberRecord["cr652_suffix"].ToString();
                    string separator = autoNumberRecord["cr652_separator"].ToString();
                    string currentNumber = autoNumberRecord["cr652_currentnumber"].ToString();
                    int temp = int.Parse(currentNumber);
                    temp++;
                    currentNumber = temp.ToString("000000");

                    //reset current number
                    autoNumberRecord["cr652_currentnumber"] = temp.ToString();
                    service.Update(autoNumberRecord);

                    //build student auto number
                    string autoNumber = prefix + separator + year + month + day + separator + suffix + separator + currentNumber;

                    studentRecord["cr652_name"] = autoNumber;

                }

            }catch(Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}
