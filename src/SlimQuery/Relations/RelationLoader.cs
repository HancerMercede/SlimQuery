using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using SlimQuery.Core;
using SlimQuery.Query.SqlDialect;

namespace SlimQuery.Relations;

public class RelationLoader(SlimConnection connection, ISqlDialect dialect)
{
    public async Task LoadRelations<T>(IEnumerable<T> entities)
    {
        var configs = RelationConfigCache.Get<T>();
        if (!configs.Any()) return;

        var entityList = entities.ToList();
        if (!entityList.Any()) return;

        foreach (var config in configs)
        {
            var pkProperty = typeof(T).GetProperty("Id");
            if (pkProperty == null) continue;

            var parentIds = entityList
                .Select(e => pkProperty.GetValue(e))
                .Where(id => id != null)
                .Cast<object>()
                .ToList();
            
            if (!parentIds.Any()) continue;

            if (config.Type == RelationType.HasMany)
            {
                await LoadHasManyAsync<T>(config, entityList, parentIds);
            }
            else
            {
                await LoadHasOneAsync<T>(config, entityList, parentIds);
            }
        }
    }

    private async Task LoadHasManyAsync<T>(RelationConfig config, List<T> entities, IReadOnlyList<object> parentIds)
    {
        if (parentIds.Count == 0) return;

        var sql = $"SELECT * FROM {dialect.EscapeIdentifier(config.ChildTable)} WHERE {config.ForeignKey} IN (@Ids)";
        var parameters = new { Ids = parentIds };

        var childEntities = await connection.QueryAsync<object>(sql, parameters);
        var childList = childEntities.ToList();

        if (childList.Count == 0) return;

        var firstChild = childList[0];
        if (firstChild == null) return;

        var fkProperty = firstChild.GetType().GetProperty(config.ForeignKey);
        var collectionProperty = typeof(T).GetProperty(config.ChildTable);

        if (fkProperty == null || collectionProperty == null) return;

        InitializeCollections<T>(entities, config.ChildTable, collectionProperty);

        var entityDict = entities.ToDictionary(e => GetId(e)!);

        foreach (var child in childList)
        {
            if (child == null) continue;

            var fkValue = fkProperty.GetValue(child);
            if (fkValue == null) continue;

            if (entityDict.TryGetValue(fkValue, out var parent))
            {
                var collection = collectionProperty.GetValue(parent) as IList<object>;
                collection?.Add(child);
            }
        }
    }

    private void InitializeCollections<T>(List<T> entities, string propertyName, PropertyInfo collectionProperty)
    {
        foreach (var entity in entities)
        {
            var collection = collectionProperty.GetValue(entity);
            if (collection == null)
            {
                var listType = typeof(List<>).MakeGenericType(typeof(object));
                collectionProperty.SetValue(entity, Activator.CreateInstance(listType));
            }
        }
    }

    private async Task LoadHasOneAsync<T>(RelationConfig config, List<T> entities, IReadOnlyList<object> parentIds)
    {
        if (parentIds.Count == 0) return;

        var sql = $"SELECT * FROM {dialect.EscapeIdentifier(config.ChildTable)} WHERE {config.ForeignKey} IN (@Ids)";
        var parameters = new { Ids = parentIds };

        var childEntities = await connection.QueryAsync<object>(sql, parameters);
        var childList = childEntities.ToList();

        if (childList.Count == 0) return;

        var firstChild = childList[0];
        if (firstChild == null) return;

        var fkProperty = firstChild.GetType().GetProperty(config.ForeignKey);
        var referenceProperty = typeof(T).GetProperty(config.ChildTable);

        if (fkProperty == null || referenceProperty == null) return;

        var entityDict = entities.ToDictionary(e => GetId(e)!);

        foreach (var child in childList)
        {
            if (child == null) continue;

            var fkValue = fkProperty.GetValue(child);
            if (fkValue == null) continue;

            if (entityDict.TryGetValue(fkValue, out var parent))
            {
                referenceProperty.SetValue(parent, child);
            }
        }
    }

    private static object? GetId(object? entity)
    {
        return entity?.GetType().GetProperty("Id")?.GetValue(entity);
    }
}
