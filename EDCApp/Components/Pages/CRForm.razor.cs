using Microsoft.AspNetCore.Components;
using EDCApp.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace EDCApp.Components.Pages
{
    public partial class CRForm
    {
        [Parameter]
        public Guid? CRFId { get; set; }

        private CRF currentCRF = new CRF {
            CRFTitle = ""
        };
        private List<CRFItem> crfItems = new List<CRFItem>();
        private List<Visit> visits = new List<Visit>(); // List to store visits
        private List<User> users = new List<User>(); // List to store users

        private bool IsEditMode => CRFId.HasValue;

        private bool showCRFItemDialog = false;
        private bool isEditingCRFItem = false;
        private CRFItem currentCRFItem = new CRFItem { FieldName = "" };
        private IOrganizationService? ServiceClient { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var dataverseUrl = Configuration["Dataverse:Url"];
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

                // Load visits and users first
                await LoadVisits();
                await LoadUsers();

                if (IsEditMode)
                {
                    await LoadCRF();
                    await LoadCRFItems();
                }
            }
            catch (MicrosoftIdentityWebChallengeUserException ex)
            {
                Console.WriteLine($"Authentication error: {ex.Message}");
                // Redirect to login or handle authentication challenge
                ConsentHandler.HandleException(ex);
            }
            catch (Exception ex)
            {
                // Handle errors gracefully
                Console.WriteLine($"Error initializing CRFs page: {ex.Message}");
                NavigationManager.NavigateTo("/login");
            }
        }

        private async Task LoadVisits()
        {
            try
            {
                if (ServiceClient == null)
                {
                    throw new InvalidOperationException("ServiceClient is not initialized.");
                }

                var query = new QueryExpression("new_visit")
                {
                    ColumnSet = new ColumnSet("new_visitid", "new_visit_number", "new_visit_date")
                };

                var result = await Task.Run(() => ServiceClient.RetrieveMultiple(query));

                visits = result.Entities.Select(entity => new Visit
                {
                    VisitId = entity.GetAttributeValue<Guid>("new_visitid"),
                    VisitNumber = entity.GetAttributeValue<string>("new_visit_number"),
                    VisitDate = entity.GetAttributeValue<DateTime>("new_visit_date"),
                    Status = entity.GetAttributeValue<OptionSetValue>("new_status")?.Value ?? 0,
                    SubjectId = entity.GetAttributeValue<Guid>("new_subject")
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load Visits: {ex.Message}");
            }
        }

        private async Task LoadUsers()
        {
            try
            {
                if (ServiceClient == null)
                {
                    throw new InvalidOperationException("ServiceClient is not initialized.");
                }

                var query = new QueryExpression("systemuser")
                {
                    ColumnSet = new ColumnSet("systemuserid", "fullname", "internalemailaddress", "firstname", "lastname")
                };

                var result = await Task.Run(() => ServiceClient.RetrieveMultiple(query));

                users = result.Entities.Select(entity => new User
                {
                    UserId = entity.GetAttributeValue<Guid>("systemuserid"),
                    UserName = entity.GetAttributeValue<string>("fullname"),
                    Email = entity.GetAttributeValue<string>("internalemailaddress"),
                    FirstName = entity.GetAttributeValue<string>("firstname"),
                    LastName = entity.GetAttributeValue<string>("lastname")
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load Users: {ex.Message}");
            }
        }

        private async Task LoadCRF()
        {
            try
            {
                if (ServiceClient == null || !CRFId.HasValue)
                {
                    throw new InvalidOperationException("ServiceClient is not initialized or CRFId is null.");
                }

                var entity = await Task.Run(() => ServiceClient.Retrieve("new_crf", CRFId.Value, 
                    new ColumnSet("new_crfid", "new_crf_title", "new_form_type", "new_completed_date", "new_visit", "new_verified_by", "createdon", "modifiedon")));

                currentCRF = new CRF
                {
                    Id = entity.GetAttributeValue<Guid>("new_crfid"),
                    CRFTitle = entity.GetAttributeValue<string>("new_crf_title"),
                    FormType = entity.GetAttributeValue<OptionSetValue>("new_form_type")?.Value ?? 0,
                    CompletedDate = entity.GetAttributeValue<DateTime?>("new_completed_date"),
                    VisitId = entity.GetAttributeValue<EntityReference>("new_visit").Id,
                    VerifiedById = entity.GetAttributeValue<EntityReference>("new_verified_by").Id,
                    CreatedOn = entity.GetAttributeValue<DateTime>("createdon"),
                    ModifiedOn = entity.GetAttributeValue<DateTime>("modifiedon")
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load CRF: {ex.Message}");
            }
        }

        private async Task LoadCRFItems()
        {
            try
            {
                if (ServiceClient == null || !CRFId.HasValue)
                {
                    throw new InvalidOperationException("ServiceClient is not initialized or CRFId is null.");
                }

                var query = new QueryExpression("new_crfitem")
                {
                    ColumnSet = new ColumnSet("new_crfitemid", "new_field_name", "new_field_value", 
                                              "new_units", "new_status", "new_crf", "createdon", "modifiedon"),
                    Criteria = new FilterExpression
                    {
                        Conditions = 
                        {
                            new ConditionExpression("new_crf", ConditionOperator.Equal, CRFId.Value)
                        }
                    }
                };

                var result = await Task.Run(() => ServiceClient.RetrieveMultiple(query));

                crfItems = result.Entities.Select(entity => new CRFItem
                {
                    Id = entity.GetAttributeValue<Guid>("new_crfitemid"),
                    FieldName = entity.GetAttributeValue<string>("new_field_name"),
                    FieldValue = entity.GetAttributeValue<string>("new_field_value"),
                    Units = entity.GetAttributeValue<string>("new_units"),
                    ItemStatus = entity.GetAttributeValue<OptionSetValue>("new_status").Value,
                    CRFId = CRFId,
                    CreatedOn = entity.GetAttributeValue<DateTime>("createdon"),
                    ModifiedOn = entity.GetAttributeValue<DateTime>("modifiedon")
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load CRF Items: {ex.Message}");
            }
        }

        // Rest of the existing methods remain the same...
        // (Previous implementation of other methods)
        private async Task HandleSubmit()
        {
            try
            {
                if (ServiceClient == null)
                {
                    throw new InvalidOperationException("ServiceClient is not initialized.");
                }

                if (IsEditMode)
                {
                    // Update existing CRF
                    var entity = new Entity("new_crf", currentCRF.Id)
                    {
                        ["new_crf_title"] = currentCRF.CRFTitle,
                        ["new_form_type"] = currentCRF.FormType,
                        ["new_completed_date"] = currentCRF.CompletedDate
                    };

                    await Task.Run(() => ServiceClient.Update(entity));
                }
                else
                {
                    // Create new CRF
                    var entity = new Entity("new_crf")
                    {
                        ["new_crf_title"] = currentCRF.CRFTitle,
                        ["new_form_type"] = currentCRF.FormType,
                        ["new_completed_date"] = currentCRF.CompletedDate
                    };

                    currentCRF.Id = await Task.Run(() => ServiceClient.Create(entity));
                }

                NavigationManager.NavigateTo("/crfs");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error submitting CRF: {ex.Message}");
            }
        }

        private void Cancel()
        {
            NavigationManager.NavigateTo("/crfs");
        }

        private string GetItemStatusDisplay(int status)
        {
            return status switch
            {
                1 => "Pending",
                2 => "Entered",
                3 => "Verified",
                _ => "Unknown"
            };
        }

        private void OpenCreateCRFItemDialog()
        {
            currentCRFItem = new CRFItem { CRFId = currentCRF.Id, FieldName = "" };
            isEditingCRFItem = false;
            showCRFItemDialog = true;
        }

        private void OpenEditCRFItemDialog(CRFItem item)
        {
            currentCRFItem = new CRFItem
            {
                Id = item.Id,
                FieldName = item.FieldName,
                FieldValue = item.FieldValue,
                Units = item.Units,
                ItemStatus = item.ItemStatus,
                CRFId = item.CRFId,
                CreatedOn = item.CreatedOn
            };
            isEditingCRFItem = true;
            showCRFItemDialog = true;
        }

        private async Task SaveCRFItem()
        {
            try
            {
                if (ServiceClient == null)
                {
                    throw new InvalidOperationException("ServiceClient is not initialized.");
                }

                if (!isEditingCRFItem)
                {
                    // Create new CRF Item
                    var entity = new Entity("new_crfitem")
                    {
                        ["new_field_name"] = currentCRFItem.FieldName,
                        ["new_field_value"] = currentCRFItem.FieldValue,
                        ["new_units"] = currentCRFItem.Units,
                        ["new_status"] = currentCRFItem.ItemStatus,
                        ["new_crf"] = currentCRFItem.CRFId
                    };

                    currentCRFItem.Id = await Task.Run(() => ServiceClient.Create(entity));
                    crfItems.Add(currentCRFItem);
                }
                else
                {
                    // Update existing CRF Item
                    var entity = new Entity("new_crfitem", currentCRFItem.Id)
                    {
                        ["new_field_name"] = currentCRFItem.FieldName,
                        ["new_field_value"] = currentCRFItem.FieldValue,
                        ["new_units"] = currentCRFItem.Units,
                        ["new_status"] = currentCRFItem.ItemStatus
                    };

                    await Task.Run(() => ServiceClient.Update(entity));

                    var existingItem = crfItems.FirstOrDefault(i => i.Id == currentCRFItem.Id);
                    if (existingItem != null)
                    {
                        var index = crfItems.IndexOf(existingItem);
                        crfItems[index] = currentCRFItem;
                    }
                }

                CloseCRFItemDialog();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving CRF Item: {ex.Message}");
            }
        }

        private async Task DeleteCRFItem(Guid crfItemId)
        {
            try
            {
                if (ServiceClient == null)
                {
                    throw new InvalidOperationException("ServiceClient is not initialized.");
                }

                await Task.Run(() => ServiceClient.Delete("new_crfitem", crfItemId));
                crfItems.RemoveAll(i => i.Id == crfItemId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting CRF Item: {ex.Message}");
            }
        }

        private void CloseCRFItemDialog()
        {
            showCRFItemDialog = false;
            currentCRFItem = new CRFItem { FieldName = "" };
        }
    }
}
