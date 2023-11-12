using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace ApplicationTestOne.Plugins
{
    public class DeleteStudent_PreValidation : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                //business logic
                ITracingService trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
                trace.Trace("Plugin: DeleteStudent_PreValidation started.");

                IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

                // validate entity
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is EntityReference)
                {
                    trace.Trace("Context has entity reference");
                    IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                    IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                    if (context.MessageName.ToLower() != "delete" && context.Stage != 10)
                    {
                        trace.Trace("Plugin step trigger not configured correctly");
                        return;
                    }
                    trace.Trace("Plugin logic starts execution");
                    //delete student
                    EntityReference deleteStudent = context.InputParameters["Target"] as EntityReference;

                    QueryExpression qeRelatedStudentCourses = new QueryExpression()
                    {
                        EntityName = "cr652_studentcourse",
                        ColumnSet = new ColumnSet("cr652_name")
                    };
                    qeRelatedStudentCourses.Criteria.AddCondition("cr652_StudentId", ConditionOperator.Equal, deleteStudent.Id);

                    //run query
                    EntityCollection ecRelated = service.RetrieveMultiple(qeRelatedStudentCourses);
                    trace.Trace("Total number of related records = " + ecRelated.Entities.Count);
                    if (ecRelated.Entities.Count > 0)
                    {
                        throw new Exception("Can not delete student because student has courses.");
                    }

                    trace.Trace("Plugin executed succussfullt buy student has zero relations.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}
