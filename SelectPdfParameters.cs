
    public class SelectPdfParameters
    {
        public string key { get; set; }
        public string url { get; set; }
        public string html { get; set; }
        public string base_url { get; set; }

        // New optional parameters
        public string page_size { get; set; }              // e.g., "A4", "Letter"
        public string page_orientation { get; set; }       // "Portrait" or "Landscape"
        public Margins margins { get; set; }
        public string horizontal_alignment { get; set; }   // "Left", "Center", "Right"
        public string vertical_alignment { get; set; }     // "Top", "Middle", "Bottom"
      public bool fit_to_paper { get; set; } // new propert

   public  bool page_breaks_enhanced_algorithm { get; set; }  // Use enhanced page break algorithm to reduce blank pages
public bool single_page_pdf { get; set; }                  // Make sure content is allowed to flow to multiple pages
}

    public class Margins
    {
        public int top { get; set; }
        public int right { get; set; }
        public int bottom { get; set; }
        public int left { get; set; }
    }
