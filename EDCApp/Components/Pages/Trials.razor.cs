using EDCApp.Models;
using EDCApp.Services;

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
        TrialService.ServiceClient = new ServiceClient(new Uri(dataverseUrl), tokenProvider, true);

        await LoadTrials();
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
        trials = await TrialService.GetTrialsAsync();
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
        if (isEditMode)
        {
          await TrialService.UpdateTrialAsync(currentTrial);
        }
        else
        {
          await TrialService.CreateTrialAsync(currentTrial);
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
          await TrialService.DeleteTrialAsync(trialToDelete.Id);
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