import { HTMLAttributes } from "react";

interface IProps extends HTMLAttributes<HTMLTableRowElement>
{
    
}

export default function TableRow(props: IProps)
{
    return <tr className="idsk-table__row">
        {props.children}
    </tr>
}