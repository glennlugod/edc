using EDCApp.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Identity.Web;

namespace EDCApp.Components.Pages
{
  public partial class Subjects
  {
    private List<Subject> subjects = new();
    private List<Trial> trials = new();
    private bool showDeleteConfirmation = false;
    private bool showCreateSubjectDialog = false;
    private bool isEditMode = false;
    private Subject currentSubject = new Subject { SubjectCode = "" }; // Initialize with a default SubjectCode
    private Subject? subjectToDelete;
    private IOrganizationService? ServiceClient { get; set; }

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
        ServiceClient = new ServiceClient(new Uri(dataverseUrl), tokenProvider, true);

        await LoadTrials();
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

    private string GetTrialName(Guid trialId)
    {
      var trial = trials.FirstOrDefault(t => t.Id == trialId);
      return trial?.TrialName ?? "Unknown Trial";
    }

    private async Task LoadTrials()
    {
      try
      {
        if (ServiceClient == null)
        {
          throw new InvalidOperationException("ServiceClient is not initialized.");
        }

        var query = new QueryExpression("new_trial")
        {
          ColumnSet = new ColumnSet("new_trialid", "new_trial_name")
        };

        var result = await Task.Run(() => ServiceClient.RetrieveMultiple(query));

        trials = result.Entities.Select(entity => new Trial
        {
          Id = entity.GetAttributeValue<Guid>("new_trialid"),
          TrialName = entity.GetAttributeValue<string>("new_trial_name")
        }).ToList();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to load trials: {ex.Message}");
      }
    }

    private async Task LoadSubjects()
    {
      try
      {
        if (ServiceClient == null)
        {
          throw new InvalidOperationException("ServiceClient is not initialized.");
        }

        var query = new QueryExpression("new_subject")
        {
          ColumnSet = new ColumnSet(
            "new_subjectid", 
            "new_subject_code", 
            "new_screening_date", 
            "new_enrollment_date", 
            "new_status", 
            "new_trial")
        };

        var result = await Task.Run(() => ServiceClient.RetrieveMultiple(query));

        subjects = result.Entities.Select(entity => new Subject
        {
          SubjectId = entity.GetAttributeValue<Guid>("new_subjectid"),
          SubjectCode = entity.GetAttributeValue<string>("new_subject_code"),
          ScreeningDate = entity.GetAttributeValue<DateTime?>("new_screening_date"),
          EnrollmentDate = entity.GetAttributeValue<DateTime?>("new_enrollment_date"),
          Status = entity.GetAttributeValue<OptionSetValue>("new_status")?.Value ?? 0,
          TrialId = entity.GetAttributeValue<EntityReference>("new_trial")?.Id ?? Guid.Empty
        }).ToList();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to load subjects: {ex.Message}");
      }
    }

    private void OpenCreateSubjectDialog()
    {
      currentSubject = new Subject
      {
        SubjectCode = "" // Ensure SubjectCode is set
      };
      isEditMode = false;
      showCreateSubjectDialog = true;
    }

    private void OpenEditSubjectDialog(Subject subject)
    {
      currentSubject = new Subject
      {
        SubjectId = subject.SubjectId,
        SubjectCode = subject.SubjectCode, // Ensure SubjectCode is set
        ScreeningDate = subject.ScreeningDate,
        EnrollmentDate = subject.EnrollmentDate,
        Status = subject.Status,
        TrialId = subject.TrialId
      };
      isEditMode = true;
      showCreateSubjectDialog = true;
    }

    private async Task SubmitSubject()
    {
      try
      {
        if (ServiceClient == null)
        {
          throw new InvalidOperationException("ServiceClient is not initialized.");
        }

        if (isEditMode)
        {
          var entity = new Entity("new_subject", currentSubject.SubjectId)
          {
            ["new_subject_code"] = currentSubject.SubjectCode,
            ["new_screening_date"] = currentSubject.ScreeningDate,
            ["new_enrollment_date"] = currentSubject.EnrollmentDate,
            ["new_status"] = new OptionSetValue(currentSubject.Status),
            ["new_trial"] = new EntityReference("new_trial", currentSubject.TrialId)
          };

          await Task.Run(() => ServiceClient.Update(entity));
        }
        else
        {
          var entity = new Entity("new_subject")
          {
            ["new_subject_code"] = currentSubject.SubjectCode,
            ["new_screening_date"] = currentSubject.ScreeningDate,
            ["new_enrollment_date"] = currentSubject.EnrollmentDate,
            ["new_status"] = new OptionSetValue(currentSubject.Status),
            ["new_trial"] = new EntityReference("new_trial", currentSubject.TrialId)
          };

          await Task.Run(() => ServiceClient.Create(entity));
        }

        await LoadSubjects();
        CloseCreateSubjectDialog();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error submitting subject: {ex.Message}");
      }
    }

    private void CloseCreateSubjectDialog()
    {
      showCreateSubjectDialog = false;
      currentSubject = new Subject { SubjectCode = "" }; // Reset with a default SubjectCode
    }

    private void OpenDeleteConfirmationDialog(Subject subject)
    {
      subjectToDelete = subject;
      showDeleteConfirmation = true;
    }

    private void CancelDelete()
    {
      showDeleteConfirmation = false;
      subjectToDelete = null;
    }

    private async Task ConfirmDelete()
    {
      if (subjectToDelete != null)
      {
        try
        {
          if (ServiceClient == null)
          {
            throw new InvalidOperationException("ServiceClient is not initialized.");
          }

          await Task.Run(() => ServiceClient.Delete("new_subject", subjectToDelete.SubjectId));
          await LoadSubjects();
          showDeleteConfirmation = false;
          subjectToDelete = null;
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Failed to delete subject: {ex.Message}");
        }
      }
    }
  }
}
