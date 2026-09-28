using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.WorkOrders;

/// <summary>Metadados de um anexo (foto/documento). O binário fica no <c>IFileStorage</c> (Fase 6).</summary>
public sealed class Attachment : BaseEntity
{
    private Attachment() { } // EF Core

    internal Attachment(Guid workOrderId, string fileName, string contentType, long sizeBytes, string storageKey,
                        string uploadedBy, DateTime uploadedAtUtc)
    {
        AttachmentId = Guid.NewGuid();
        WorkOrderId = workOrderId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StorageKey = storageKey;
        UploadedBy = uploadedBy;
        UploadedAt = uploadedAtUtc;
    }

    public Guid AttachmentId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public string FileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = null!;
    public string UploadedBy { get; private set; } = null!;
    public DateTime UploadedAt { get; private set; }
}
