namespace eShopWCFService.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using System.Runtime.Serialization;

    [DataContract]
    public partial class CatalogBrand
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [DataMember]
        public int Id { get; set; }

        [StringLength(50)]
        [DataMember]
        public string Brand { get; set; }
    }
}
