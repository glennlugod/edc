using EDCApp.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace EDCApp.Components.Pages
{
    public partial class Visits
    {
        private IOrganizationService? organizationService;

        private List<Visit> visits = new();
        private List<Subject> subjects = new();
        private Visit currentVisit = new Visit();
        private bool ShowModal { get; set; }
        private bool IsEditing { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var dataverseUrl = Config["Dataverse:Url"];
                if (dataverseUrl == null)
                {
                throw new InvalidOperationException("Dataverse:Url is not configured.");
                }

                var accessToken = await TokenAcquisition
                    .GetAccessTokenForUserAsync(
                        new[] { $"{dataverseUrl}/.default" });

                // Create token provider function for Dataverse
                Func<string, Task<string>> tokenProvider = async (resource) =>
                {
                return await Task.FromResult(accessToken);
                };

                // Connect to Dataverse
                organizationService = new ServiceClient(new Uri(dataverseUrl), tokenProvider, true);

                await LoadVisits();
                await LoadSubjects();
            }
            catch (MicrosoftIdentityWebChallengeUserException ex)
            {
                Console.WriteLine($"Authentication error: {ex.Message}");
                ConsentHandler.HandleException(ex);        
            }
            catch (Exception ex)
            {
                // Handle errors gracefully
                Console.WriteLine($"Error connecting to Dataverse: {ex.Message}");
            }            
        }

        private async Task LoadVisits()
        {
            if (organizationService == null)
            {
                throw new InvalidOperationException("OrganizationService is not initialized.");
            }

            var query = new QueryExpression("new_visit")
            {
                EntityName = "new_visit",
                ColumnSet = new ColumnSet(
                    "new_visitid", 
                    "new_visit_number", 
                    "new_visit_date", 
                    "new_status", 
                    "new_subject")
            };

            var result = await Task.Run(() => organizationService.RetrieveMultiple(query));

            visits = result.Entities.Select(e => new Visit
            {
                VisitId = e.Id,
                VisitNumber = e.GetAttributeValue<string>("new_visit_number"),
                VisitDate = e.GetAttributeValue<DateTime>("new_visit_date"),
                Status = e.GetAttributeValue<OptionSetValue>("new_status")?.Value ?? 0,
                SubjectId = e.GetAttributeValue<EntityReference>("new_subject")?.Id ?? Guid.Empty
            }).ToList();
        }

        private async Task LoadSubjects()
        {
            if (organizationService == null)
            {
                throw new InvalidOperationException("OrganizationService is not initialized.");
            }
            
            var query = new QueryExpression("new_subject")
            {
                EntityName = "new_subject",
                ColumnSet = new ColumnSet(
                    "new_subjectid", 
                    "new_subject_code", 
                    "new_screening_date", 
                    "new_enrollment_date", 
                    "new_status", 
                    "new_trial")
            };

            var result = await Task.Run(() => organizationService.RetrieveMultiple(query));
            
            subjects = result.Entities.Select(e => new Subject
            {
                SubjectId = e.Id,
                SubjectCode = e.GetAttributeValue<string>("new_subject_code") ?? string.Empty,
                TrialId = e.GetAttributeValue<EntityReference>("new_trial")?.Id ?? Guid.Empty,
                Status = e.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0
            }).ToList();
        }

        protected void CreateVisit()
        {
            currentVisit = new Visit
            {
                VisitId = Guid.NewGuid(),
                VisitDate = DateTime.Now,
                Status = 1 // Default to Planned
            };
            IsEditing = false;
            ShowModal = true;
        }

        protected void EditVisit(Visit visit)
        {
            currentVisit = new Visit
            {
                VisitId = visit.VisitId,
                VisitNumber = visit.VisitNumber,
                VisitDate = visit.VisitDate,
                Status = visit.Status,
                SubjectId = visit.SubjectId
            };
            IsEditing = true;
            ShowModal = true;
        }

        protected async Task SaveVisit()
        {
            if (organizationService == null)
            {
                throw new InvalidOperationException("OrganizationService is not initialized.");
            }
            
            var entity = new Entity("new_visit", currentVisit.VisitId);
            
            entity["new_visit_number"] = currentVisit.VisitNumber;
            entity["new_visit_date"] = currentVisit.VisitDate;
            entity["new_status"] = new OptionSetValue(currentVisit.Status);
            
            if (currentVisit.SubjectId != Guid.Empty)
            {
                entity["new_subject"] = new EntityReference("new_subject", currentVisit.SubjectId);
            }

            if (IsEditing)
            {
                organizationService.Update(entity);
            }
            else
            {
                organizationService.Create(entity);
            }

            await LoadVisits();
            ShowModal = false;
        }

        protected async Task DeleteVisit(Visit visit)
        {
            if (organizationService == null)
            {
                throw new InvalidOperationException("OrganizationService is not initialized.");
            }
            
            organizationService.Delete("new_visit", visit.VisitId);
            await LoadVisits();
        }

        protected void CloseModal()
        {
            ShowModal = false;
        }

        protected string GetStatusText(int status)
        {
            return status switch
            {
                1 => "Planned",
                2 => "In Progress",
                3 => "Completed",
                _ => "Unknown"
            };
        }

        protected string GetSubjectName(Guid subjectId)
        {
            var subject = subjects?.FirstOrDefault(s => s.SubjectId == subjectId);
            return subject != null 
                ? $"{subject.SubjectCode}" 
                : "N/A";
        }
    }
}
