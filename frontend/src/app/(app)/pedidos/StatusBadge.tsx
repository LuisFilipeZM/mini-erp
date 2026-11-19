import { statusLabel } from "@/lib/format";
import type { OrderStatus } from "@/lib/types";
import { Badge } from "@/components/ui";

const colors = { Open: "amber", Confirmed: "green", Cancelled: "gray" } as const;

export function StatusBadge({ status }: { status: OrderStatus }) {
  return <Badge color={colors[status]}>{statusLabel[status]}</Badge>;
}
