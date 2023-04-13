using System;
using Newtonsoft.Json;

namespace TNRD.Zeepkist.GTR.Auth.Directus.Models
{
    public class BaseDirectusModel : IEquatable<BaseDirectusModel>
    {
        [JsonProperty("id")] private int? id;
        [JsonProperty("date_created")] private DateTime? dateCreated;
        [JsonProperty("date_updated")] private DateTime? dateUpdated;

        [JsonIgnore] public int Id => id!.Value;
        [JsonIgnore] public DateTime DateCreated => dateCreated!.Value;
        [JsonIgnore] public DateTime? DateUpdated => dateUpdated;

        /// <inheritdoc />
        public bool Equals(BaseDirectusModel? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return id == other.id;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BaseDirectusModel)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return id.GetHashCode();
        }
    }
}
