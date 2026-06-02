"use client";

import { type ReactNode } from "react";
import { cn } from "@/lib/utils";

export interface DataTableRow {
  id: string | number;
  cells: ReactNode[];
}

export function DataTable({
  columns,
  rows,
  empty,
  className,
}: {
  columns: { key: string; header: ReactNode; className?: string }[];
  rows: DataTableRow[];
  empty?: ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-soft", className)}>
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-slate-200 text-sm">
          <thead className="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
            <tr>
              {columns.map((c) => (
                <th key={c.key} scope="col" className={cn("px-4 py-3", c.className)}>
                  {c.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {rows.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="px-4 py-10 text-center text-slate-500">
                  {empty ?? "No records found."}
                </td>
              </tr>
            ) : (
              rows.map((row) => (
                <tr key={row.id} className="hover:bg-slate-50/60">
                  {row.cells.map((cell, i) => (
                    <td key={i} className={cn("px-4 py-3 align-middle", columns[i]?.className)}>
                      {cell}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
