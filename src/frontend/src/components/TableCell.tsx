import { HTMLAttributes } from "react";

interface IProps extends HTMLAttributes<HTMLTableCellElement>
{
    
}

export default function TableCell(props: IProps)
{
    return <td className="idsk-table__cell" {...props}>
        {props.children}
    </td>
}