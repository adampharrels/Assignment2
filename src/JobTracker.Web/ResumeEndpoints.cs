using JobTracker.Data.Profile;

namespace JobTracker.Web;

/// <summary>Serves the uploaded resume file to the browser (G6, Grace).</summary>
public static class ResumeEndpoints
{
    /// <summary>The address of the resume file. Add "?download=true" to save a PDF instead of opening it.</summary>
    public const string Url = "/profile/resume";

    /// <summary>Maps GET /profile/resume: PDFs open in the browser; other types (and ?download=true) are downloaded.</summary>
    public static IEndpointRouteBuilder MapResumeFile(this IEndpointRouteBuilder app)
    {
        app.MapGet(Url, (ResumeFileStore store, bool? download) =>
        {
            if (store.Open() is not var (content, resume))
            {
                return Results.NotFound("No resume has been uploaded.");
            }

            // Giving a download name makes the browser save the file instead of showing it.
            bool openInBrowser = resume.CanOpenInBrowser && download != true;
            return Results.File(content, resume.ContentType, openInBrowser ? null : resume.FileName,
                lastModified: resume.UploadedAt, enableRangeProcessing: true);
        });

        return app;
    }
}
