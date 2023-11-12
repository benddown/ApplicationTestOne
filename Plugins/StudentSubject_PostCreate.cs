using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;


namespace ApplicationTestOne.Plugins
{
    public class StudentSubject_PostCreate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                // business logic goes here

                // get context from service provider
                IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

                // load trace service
                ITracingService trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                // validate context to be for entity component
                if(context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    // set organisation params
                    IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                    IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                    // validate plugin trigger
                    if(context.MessageName.ToLower() != "create" && context.Stage != 40)
                    {
                        return;
                    }

                    // plugin logic

                    //get guid of created record since its a post operation
                    Guid studentRecordId = Guid.Parse(context.OutputParameters["id"].ToString());

                    //Retrieve created record as entity record object
                    Entity studentRecord = service.Retrieve("cr652_student", studentRecordId, new ColumnSet("cr652_firstname", "cr652_name", "cr652_cycle"));

                    // validate created record
                    if(studentRecord != null)
                    {
                        //trace.Trace("First name of student record created: " + studentRecord["cr652_firstname"]);

                        int cycle = (int)((OptionSetValue)studentRecord["cr652_cycle"]).Value;
                        switch (cycle)
                        {
                            case (int)Constants.Constants.Cycle.P:
                                createStudentCourseObject(service, trace, studentRecordId, studentRecord, true);
                                break;

                            case (int)Constants.Constants.Cycle.C:
                                createStudentCourseObject(service, trace, studentRecordId, studentRecord, false);
                                break;

                            default:
                                trace.Trace("No record has been created!");
                                break;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }

        private void createStudentCourseObject(IOrganizationService service, 
            ITracingService trace, Guid studentRecordId, Entity studentRecord, bool isPCycle)
        {
            trace.Trace("creating the student course record!");
            // query to retrive course records
            QueryExpression qeCourses = new QueryExpression() { 
                EntityName = "cr652_course",
                ColumnSet = new ColumnSet("cr652_name")
            };
            
            // filter only semester one courses
            qeCourses.Criteria.AddCondition("cr652_semester", ConditionOperator.Equal, (int)Constants.Constants.Semesters.Semester1);

            // filter by cycle depending on PCycle argument
            if (isPCycle)
            {
                qeCourses.Criteria.AddCondition("cr652_cycle", ConditionOperator.In, (int)Constants.Constants.Cycle.P, (int)Constants.Constants.Cycle.both);
                trace.Trace("P cycle student courses created successfully");
            }
            else
            {
                qeCourses.Criteria.AddCondition("cr652_cycle", ConditionOperator.In, (int)Constants.Constants.Cycle.C, (int)Constants.Constants.Cycle.both);
                trace.Trace("Q cycle student courses created successfully");
            }

            // retrive valid records
            EntityCollection ecCourseRecords = service.RetrieveMultiple(qeCourses);

            trace.Trace(ecCourseRecords.Entities.Count + " : have been retrieved.");

            // validate if records passing criteria exist
            if(ecCourseRecords.Entities.Count != 0)
            {
                foreach(Entity entity in ecCourseRecords.Entities)
                {
                    Entity createStudentCourseRecord = new Entity("cr652_studentcourse");

                    createStudentCourseRecord["cr652_name"] = studentRecord["cr652_name"].ToString() + " - " + entity["cr652_name"].ToString();

                    createStudentCourseRecord["cr652_course"] = new EntityReference("cr652_course", entity.Id);

                    createStudentCourseRecord["cr652_student"] = new EntityReference("cr652_student", studentRecordId);

                    trace.Trace("created record with name: " + studentRecord["cr652_name"].ToString() + " - " + entity["cr652_name"].ToString());

                    service.Create(createStudentCourseRecord);
                }
            }
            else
            {
                throw new Exception("no record matches criteria for semester and cycle");
            }
        }
    }
}
