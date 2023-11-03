using System;
using System.Diagnostics;
using System.Linq.Expressions;
using ManeroBackendAPI.Contexts;
using Microsoft.EntityFrameworkCore;


namespace ManeroBackendAPI.Repositories;

public interface IRepository<TEntity, TbContext> where TEntity : class where TbContext : DbContext
{
    Task<TEntity> CreateAsync(TEntity entity);
    Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate);
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> expression);

    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> GetAllByConditionsAsync(Expression<Func<TEntity, bool>> predicate);
    Task<TEntity> UpdateAsync(Expression<Func<TEntity, bool>> predicate, TEntity entity);
    Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> predicate);
}

public abstract class Repository<TEntity, TbContext> : IRepository<TEntity, ApplicationDBContext> where TEntity : class
{
    protected readonly ApplicationDBContext _context;

    protected Repository(ApplicationDBContext context)
    {
        _context = context;
    }
    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> expression)
    {
        try
        {
            return await _context.Set<TEntity>().AnyAsync(expression);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return false;
        }
    }

    public virtual async Task<TEntity> CreateAsync(TEntity entity)
    {
        try
        {
            await _context.Set<TEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            throw;
        }
    }



    public virtual async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            return await _context.Set<TEntity>().FirstOrDefaultAsync(predicate) ?? null!;


        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return null!;
        }
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        try
        {
            return await _context.Set<TEntity>().ToListAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return new List<TEntity>();
        }
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllByConditionsAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            return await _context.Set<TEntity>().Where(predicate).ToListAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return new List<TEntity>();
        }
    }


    public virtual async Task<TEntity> UpdateAsync(Expression<Func<TEntity, bool>> predicate, TEntity entity)
    {
        try
        {
            var existingEntity = await _context.Set<TEntity>().FirstOrDefaultAsync(predicate);
            if (existingEntity != null)
            {
                _context.Entry(existingEntity).CurrentValues.SetValues(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
            return null!;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return null!;
        }
    }

    public virtual async Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            var entityToDelete = await _context.Set<TEntity>().FirstOrDefaultAsync(predicate);
            if (entityToDelete != null)
            {
                _context.Set<TEntity>().Remove(entityToDelete);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return false;
        }
    }


}
