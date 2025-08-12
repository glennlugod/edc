using EDCApp.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace EDCApp.Components.Pages
{
    public partial class CRFs
    {
        private List<CRF> crfs = new();
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

                await LoadCRFs();
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

        private async Task LoadCRFs()
        {
            try
            {
                if (ServiceClient == null)
                {
                    throw new InvalidOperationException("ServiceClient is not initialized.");
                }

                var query = new QueryExpression("new_crf")
                {
                    ColumnSet = new ColumnSet(
                        "new_crfid", 
                        "new_crf_title", 
                        "new_form_type", 
                        "new_completed_date", 
                        "createdon")
                };

                var result = await Task.Run(() => ServiceClient.RetrieveMultiple(query));

                crfs = result.Entities.Select(entity => new CRF
                {
                    Id = entity.GetAttributeValue<Guid>("new_crfid"),
                    CRFTitle = entity.GetAttributeValue<string>("new_crf_title"),
                    FormType = entity.GetAttributeValue<OptionSetValue>("new_form_type").Value,
                    CompletedDate = entity.GetAttributeValue<DateTime?>("new_completed_date"),
                    CreatedOn = entity.GetAttributeValue<DateTime>("createdon")
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load CRFs: {ex.Message}");
            }
        }

        private void NavigateToCRForm(CRF crf)
        {
            // Navigate to CRForm with the selected CRF's ID
            NavigationManager.NavigateTo($"/crform/edit/{crf.Id}");
        }
    }
}
