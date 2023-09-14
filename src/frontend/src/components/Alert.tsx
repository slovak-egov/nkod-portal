import { PropsWithChildren } from "react";

interface IProps extends PropsWithChildren
{
    type: 'info' | 'warning'
}

export default function Alert(props: IProps)
{
    return <div className={'idsk-warning-text ' + (props.type === 'info' && 'idsk-warning-text--info')} {...props}>
        <div className="govuk-width-container">
            <div className="idsk-warning-text__text">
                {props.children}
            </div>
        </div>
    </div>
}

Alert.defaultProps = {
    type: 'info'
}