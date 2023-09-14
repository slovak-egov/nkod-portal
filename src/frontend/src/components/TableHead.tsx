import { HTMLAttributes } from "react";

interface IProps extends HTMLAttributes<HTMLTableSectionElement>
{
    
}

export default function TableHead(props: IProps)
{
    return <thead className="idsk-table__head">
        {props.children}
    </thead>
}