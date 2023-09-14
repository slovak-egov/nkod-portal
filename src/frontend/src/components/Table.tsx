import { HTMLAttributes } from "react";
import IdSkModule from "./IdSkModule";

interface IProps extends HTMLAttributes<HTMLTableElement>
{
    
}

export default function Table(props: IProps)
{
    return <IdSkModule moduleType="idsk-table"><table className="idsk-table">
        {props.children}
    </table></IdSkModule>;
}