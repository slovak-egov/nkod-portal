import { HTMLAttributes } from "react";

type Props = HTMLAttributes<HTMLTableRowElement>

export default function TableRow(props: Props)
{
    return <tr className="idsk-table__row">
        {props.children}
    </tr>
}