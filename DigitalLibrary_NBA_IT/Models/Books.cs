//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DigitalLibrary_NBA_IT.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Books
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Books()
        {
            this.Authors = new HashSet<Authors>();
            this.CART = new HashSet<CART>();
            this.Genres = new HashSet<Genres>();
            this.Reviews = new HashSet<Reviews>();
            this.UserLibrary = new HashSet<UserLibrary>();
            this.WAITLIST = new HashSet<WAITLIST>();
        }
    
        public string Book_ID { get; set; }
        public string Title { get; set; }
        public string Publish { get; set; }
        public string Price { get; set; }
        public string CopiesAvailable { get; set; }
        public string ImageUrl { get; set; }
        public Nullable<int> age { get; set; }
        public string OriginalPrice { get; set; }
        public Nullable<System.DateTime> DiscountStartDate { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Authors> Authors { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CART> CART { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Genres> Genres { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Reviews> Reviews { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<UserLibrary> UserLibrary { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<WAITLIST> WAITLIST { get; set; }
    }
}
