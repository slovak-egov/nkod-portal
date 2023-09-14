import { ReactNode } from "react"

interface IProps<T>
{
    items: T[];
    renderer: (item: T) => ReactNode;
    noItemsContent: ReactNode;
}

export default function ContentList<T>(props: IProps<T>)
{
    return <>{props.items.length > 0 ? <div>
        {props.items.map((item, index) => <div key={index}>
            {props.renderer(item)}
            <hr className="govuk-line" aria-hidden="true"/>
        </div>)}
    </div> : props.noItemsContent}</>
}