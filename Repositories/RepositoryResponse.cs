namespace ManeroBackendAPI.Repositories;

public class RepositoryResponse<TEntity>
{
    public TEntity Content { get; set; }
    public bool IsSuccessful { get; set; }
    public string ErrorMessage { get; set; }
}
