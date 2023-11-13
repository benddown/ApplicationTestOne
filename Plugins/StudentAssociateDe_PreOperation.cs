using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace ApplicationTestOne.Plugins
{
    public class StudentAssociateDe_PreOperation : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                // business logic
                IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

                ITracingService trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
                trace.Trace("Associate plugin started.");
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                // declare variables
                EntityReference erTargetEntityStudent = null;
                EntityReference erRelatedEntityClass = null;
                string relationshipName = string.Empty;
                Entity studentEntity = null;
                Entity courseEntity = null;
                Entity classEntity = null;

                if(context.InputParameters.Contains("Target") && context.InputParameters["Target"] is EntityReference)
                {
                    trace.Trace("Plugin registration validated successfully and student record generated!");
                    //get target entity from context
                    erTargetEntityStudent = context.InputParameters["Target"] as EntityReference;
                }

                // check relationship in context
                if (context.InputParameters.Contains("Relationship"))
                {
                    trace.Trace("Context has relationship");
                    relationshipName = ((Relationship)context.InputParameters["Relationship"]).SchemaName;
                }

                // validate schema name in context
                if(relationshipName != "cr652_Student_cr652_Class_cr652_Class")
                {
                    trace.Trace("Invalid relationship name in context: " + relationshipName);
                    return;
                }

                // check context for related entities
                if(context.InputParameters.Contains("RelatedEntities") && context.InputParameters["RelatedEntities"] is EntityReferenceCollection)
                {
                    trace.Trace("Context has entity reference collection");
                    EntityReferenceCollection ercRelated = context.InputParameters["RelatedEntities"] as EntityReferenceCollection;

                    erRelatedEntityClass = ercRelated[0];
                    trace.Trace("Selected related entity: " + erRelatedEntityClass.Name);
                }

                //generate entities
                studentEntity = service.Retrieve(erTargetEntityStudent.LogicalName, erTargetEntityStudent.Id,
                    new ColumnSet("cr652_basicfee", "cr652_course", "cr652_tax", "cr652_totalfee", "cr652_university"));

                classEntity = service.Retrieve(erRelatedEntityClass.LogicalName, erRelatedEntityClass.Id, new ColumnSet("cr652_course"));

                courseEntity = service.Retrieve("cr652_course", classEntity.GetAttributeValue<EntityReference>("cr652_course").Id, 
                    new ColumnSet("cr652_basicfee"));

                // validate plugin message
                if(context.MessageName.ToLower() == "associate")
                {
                    // verify student and class course
                    if(studentEntity.GetAttributeValue<EntityReference>("cr652_course").Id != classEntity.GetAttributeValue<EntityReference>("cr652_course").Id)
                    {
                        throw new Exception("The student course and class course do not match.");
                    }


                }else if(context.MessageName.ToLower() == "disassociate")
                {
                    // run code
                }


            }
            catch(Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}
