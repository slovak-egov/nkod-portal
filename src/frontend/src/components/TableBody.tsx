import { HTMLAttributes } from "react";

type Props = HTMLAttributes<HTMLTableSectionElement>

export default function TableBody(props: Props)
{
    return <tbody className="idsk-table__body">
        {props.children}
    </tbody>
}