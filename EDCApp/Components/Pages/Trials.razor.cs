using EDCApp.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace EDCApp.Components.Pages
{
  public partial class Trials
  {
    private List<Trial> trials = new();
    private bool showDeleteConfirmation = false;
    private bool showCreateTrialDialog = false;
    private bool isEditMode = false;
    private Trial currentTrial = new Trial { TrialName = "" }; // Initialize with a default TrialName
    private Trial? trialToDelete;
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
          ColumnSet = new ColumnSet("new_trialid", "new_trial_name", "new_sponsor", "new_start_date", "new_end_date")
        };

        var result = await Task.Run(() => ServiceClient.RetrieveMultiple(query));

        trials = result.Entities.Select(entity => new Trial
        {
          Id = entity.GetAttributeValue<Guid>("new_trialid"),
          TrialName = entity.GetAttributeValue<string>("new_trial_name"),
          Sponsor = entity.GetAttributeValue<string>("new_sponsor"),
          StartDate = entity.GetAttributeValue<DateTime?>("new_start_date"),
          EndDate = entity.GetAttributeValue<DateTime?>("new_end_date")
        }).ToList();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to load trials: {ex.Message}");
      }
    }

    private void OpenCreateTrialDialog()
    {
      currentTrial = new Trial
      {
        TrialName = "" // Ensure TrialName is set
      };
      isEditMode = false;
      showCreateTrialDialog = true;
    }

    private void OpenEditTrialDialog(Trial trial)
    {
      currentTrial = new Trial
      {
        Id = trial.Id,
        TrialName = trial.TrialName, // Ensure TrialName is set
        Sponsor = trial.Sponsor,
        StartDate = trial.StartDate,
        EndDate = trial.EndDate
      };
      isEditMode = true;
      showCreateTrialDialog = true;
    }

    private async Task SubmitTrial()
    {
      try
      {
        if (ServiceClient == null)
        {
          throw new InvalidOperationException("ServiceClient is not initialized.");
        }

        if (isEditMode)
        {
          var entity = new Entity("new_trial", currentTrial.Id)
          {
            ["new_trial_name"] = currentTrial.TrialName,
            ["new_sponsor"] = currentTrial.Sponsor,
            ["new_start_date"] = currentTrial.StartDate,
            ["new_end_date"] = currentTrial.EndDate
          };

          await Task.Run(() => ServiceClient.Update(entity));
        }
        else
        {
          var entity = new Entity("new_trial")
          {
            ["new_trial_name"] = currentTrial.TrialName,
            ["new_sponsor"] = currentTrial.Sponsor,
            ["new_start_date"] = currentTrial.StartDate,
            ["new_end_date"] = currentTrial.EndDate
          };

          await Task.Run(() => ServiceClient.Create(entity));
        }

        await LoadTrials();
        CloseCreateTrialDialog();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error submitting trial: {ex.Message}");
      }
    }

    private void CloseCreateTrialDialog()
    {
      showCreateTrialDialog = false;
      currentTrial = new Trial { TrialName = "" }; // Reset with a default TrialName
    }

    private void OpenDeleteConfirmationDialog(Trial trial)
    {
      trialToDelete = trial;
      showDeleteConfirmation = true;
    }

    private void CancelDelete()
    {
      showDeleteConfirmation = false;
      trialToDelete = null;
    }

    private async Task ConfirmDelete()
    {
      if (trialToDelete != null)
      {
        try
        {
          if (ServiceClient == null)
          {
            throw new InvalidOperationException("ServiceClient is not initialized.");
          }

          await Task.Run(() => ServiceClient.Delete("new_trial", trialToDelete.Id));
          await LoadTrials();
          showDeleteConfirmation = false;
          trialToDelete = null;
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Failed to delete trial: {ex.Message}");
        }
      }
    }
  }
}
