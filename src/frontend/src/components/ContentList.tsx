import { ReactNode } from "react"

type Props<T> =
{
    items: T[];
    renderer: (item: T) => ReactNode;
    noItemsContent: ReactNode;
}

export default function ContentList<T>(props: Props<T>)
{
    return <>{props.items.length > 0 ? <div>
        {props.items.map((item, index) => <div key={index}>
            {props.renderer(item)}
            <hr className="govuk-line" aria-hidden="true"/>
        </div>)}
    </div> : props.noItemsContent}</>
}