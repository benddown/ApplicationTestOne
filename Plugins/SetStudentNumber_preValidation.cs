using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace ApplicationTestOne.Plugins
{
    public class SetStudentNumber_preValidation : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                // business logic
                IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

                if(context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    ITracingService trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
                    IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                    IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                    if(context.MessageName.ToString() != "create" && context.Stage != 10)
                    {
                        return;
                    }

                    // create auto number
                    DateTime today = DateTime.Now;
                    string year = today.Year.ToString();
                    string month = today.Month.ToString("00");
                    string day = today.Day.ToString("00");

                    QueryExpression qeAutoNumberConfig = new QueryExpression()
                    {
                        EntityName = "cr652_applicationautonumberconfig",
                        ColumnSet = new ColumnSet("cr652_currentnumber", "cr652_name", "cr652_prefix", "cr652_separator", "cr652_suffix")
                    };

                    qeAutoNumberConfig.Criteria.AddCondition("cr652_name", ConditionOperator.Equal, "student auto number");

                    //query record
                    EntityCollection ecAutoNumberFields = service.RetrieveMultiple(qeAutoNumberConfig);

                    if(ecAutoNumberFields.Entities.Count > 0)
                    {
                        Entity autoNumberField = ecAutoNumberFields.Entities[0];
                        string prefix = autoNumberField["cr652_prefix"].ToString();
                        string suffix = autoNumberField["cr652_suffix"].ToString();
                        string separator = autoNumberField["cr652_separator"].ToString();
                        string currentNumber = autoNumberField["cr652_currentnumber"].ToString();
                        int temp = int.Parse(currentNumber);
                        temp++;
                        currentNumber = temp.ToString("000000");
                        //autoNumberField["cr652_currentnumber"] = temp.ToString();

                        string autoNumber = prefix + separator + year + month + day + separator + suffix + separator + currentNumber;

                        QueryExpression qeStudentRecords = new QueryExpression()
                        {
                            EntityName = "cr652_student",
                            ColumnSet = new ColumnSet("cr652_name")
                        };

                        qeStudentRecords.Criteria.AddCondition("cr652_name", ConditionOperator.Equal, autoNumber);

                        EntityCollection ecStudents = service.RetrieveMultiple(qeStudentRecords);

                        if(ecStudents.Entities.Count > 0)
                        {
                            throw new Exception("Duplicate record found: " + autoNumber);
                        }
                    }
                }

            }catch(Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}
