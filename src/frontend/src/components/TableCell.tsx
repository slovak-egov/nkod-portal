import { HTMLAttributes } from "react";

type Props = HTMLAttributes<HTMLTableCellElement>

export default function TableCell(props: Props)
{
    return <td className="idsk-table__cell" {...props}>
        {props.children}
    </td>
}