namespace ImportControlTower.Domain.Enums;

public static class ImportCaseStatus
{
    public const string Draft = "Draft";
    public const string Active = "Active";
    public const string Closed = "Closed";
    public const string Cancelled = "Cancelled";
}

public static class ProductionStatus
{
    public const string NotStarted = "NotStarted";
    public const string Started = "Started";
    public const string Delayed = "Delayed";
    public const string Completed = "Completed";
    public const string ReadyForShipment = "ReadyForShipment";
}

public static class TransportMode
{
    public const string Sea = "Sea";
    public const string Air = "Air";
    public const string Road = "Road";
    public const string Rail = "Rail";
    public const string Courier = "Courier";
    public const string Multimodal = "Multimodal";
}

public static class ImportCaseLineStatus
{
    public const string Allocated = "Allocated";
    public const string PartiallyShipped = "PartiallyShipped";
    public const string FullyShipped = "FullyShipped";
    public const string Cancelled = "Cancelled";
}

public static class ShipmentStatus
{
    public const string Draft = "Draft";
    public const string BookingPending = "BookingPending";
    public const string Booked = "Booked";
    public const string Loading = "Loading";
    public const string InTransit = "InTransit";
    public const string Arrived = "Arrived";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
    public const string Aborted = "Aborted";
}

public static class ShipmentLineAllocationStatus
{
    public const string Allocated = "Allocated";
    public const string Shipped = "Shipped";
    public const string Received = "Received";
    public const string Cancelled = "Cancelled";
}

public static class ContainerType
{
    public const string GP20 = "20GP";
    public const string GP40 = "40GP";
    public const string HC40 = "40HC";
    public const string HC45 = "45HC";
    public const string LCL = "LCL";
    public const string Reefer = "Reefer";
    public const string OpenTop = "OpenTop";
    public const string FlatRack = "FlatRack";
    public const string Other = "Other";
}

public static class ContainerStatus
{
    public const string Assigned = "Assigned";
    public const string Loaded = "Loaded";
    public const string InTransit = "InTransit";
    public const string Discharged = "Discharged";
    public const string Delivered = "Delivered";
    public const string Returned = "Returned";
    public const string Cancelled = "Cancelled";
}

public static class MilestoneStatus
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Skipped = "Skipped";
    public const string Cancelled = "Cancelled";
}

public static class MilestoneSource
{
    public const string Manual = "Manual";
    public const string SystemDerived = "SystemDerived";
}

public static class IdempotencyRequestStatus
{
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}
