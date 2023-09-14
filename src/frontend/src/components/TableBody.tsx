import { HTMLAttributes } from "react";

interface IProps extends HTMLAttributes<HTMLTableSectionElement>
{
    
}

export default function TableBody(props: IProps)
{
    return <tbody className="idsk-table__body">
        {props.children}
    </tbody>
}