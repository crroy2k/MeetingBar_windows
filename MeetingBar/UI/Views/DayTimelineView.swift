//
//  DayTimelineView.swift
//  MeetingBar
//
//  Created by Andrii Leitsius on 22.05.2025.
//  Copyright © 2025 Andrii Leitsius. All rights reserved.
//

import AppKit
import SwiftUI

struct DaySegment: Identifiable {
    let id: String
    let start: Date
    let end: Date
    let color: Color
    let isHighlighted: Bool
    let title: String?

    init(
        id: String = UUID().uuidString,
        start: Date,
        end: Date,
        color: Color,
        isHighlighted: Bool = false,
        title: String? = nil
    ) {
        self.id = id
        self.start = start
        self.end = end
        self.color = color
        self.isHighlighted = isHighlighted
        self.title = title
    }
}

enum DayTimelineLayout {
    static let baseTrackHeight: CGFloat = 22
    static let segmentHeight: CGFloat = 10
    static let rowSpacing: CGFloat = 4

    static var rowHeight: CGFloat { segmentHeight + rowSpacing }
}

struct DayTimelineLayoutCalculator {
    // MARK: Cached range information
    let now: Date
    let visibleRange: ClosedRange<Date>
    private let totalSeconds: TimeInterval

    init(now: Date = Date(), hoursBefore: Int = 3, hoursAfter: Int = 6) {
        self.now = now
        let lower = now.addingTimeInterval(TimeInterval(-max(0, hoursBefore) * 3600))
        let upper = now.addingTimeInterval(TimeInterval(max(1, hoursAfter) * 3600))
        self.visibleRange = lower...upper
        self.totalSeconds = upper.timeIntervalSince(lower)
    }

    // MARK: X-position helpers
    func xPosition(of date: Date, width: CGFloat) -> CGFloat {
        let clamped = min(max(date, visibleRange.lowerBound), visibleRange.upperBound)
        let seconds = clamped.timeIntervalSince(visibleRange.lowerBound)
        return width * CGFloat(seconds / totalSeconds)
    }

    // MARK: Hour ticks
    func hourTicks() -> [Date] {
        var out: [Date] = []
        let visibleHours = totalSeconds / 3600
        let tickHours: Int
        switch visibleHours {
        case ...12: tickHours = 1
        case ...24: tickHours = 2
        case ...48: tickHours = 4
        default: tickHours = 6
        }
        var current = Calendar.current.nextDate(
            after: visibleRange.lowerBound.addingTimeInterval(-1),
            matching: DateComponents(minute: 0, second: 0),
            matchingPolicy: .strict
        )!
        while current <= visibleRange.upperBound {
            out.append(current)
            current = Calendar.current.date(byAdding: .hour, value: tickHours, to: current)!
        }
        return out
    }

    // MARK: Row packing
    func rows(for segments: [DaySegment]) -> [[DaySegment]] {
        var rows: [[DaySegment]] = []
        for seg in segments where seg.end > visibleRange.lowerBound && seg.start < visibleRange.upperBound {
            if let idx = rows.firstIndex(where: { row in
                !row.contains(where: { $0.start < seg.end && $0.end > seg.start })
            }) {
                rows[idx].append(seg)
            } else {
                rows.append([seg])
            }
        }
        return rows
    }
}

struct DayRelativeTimelineView: View {
    let segments: [DaySegment]
    let currentDate: Date
    let timeFormat: TimeFormat
    let hoursBefore: Int
    let hoursAfter: Int
    var onOpen: (() -> Void)?

    // Cached / pre-computed values
    private let layout: DayTimelineLayoutCalculator
    private let eventRows: [[DaySegment]]
    private let contentHeight: CGFloat

    /// Height the parent can rely on for sizing
    var preferredHeight: CGFloat { contentHeight + 26 }   // top labels + vertical padding

    // MARK: Init
    init(
        segments: [DaySegment],
        currentDate: Date,
        timeFormat: TimeFormat,
        hoursBefore: Int = 3,
        hoursAfter: Int = 6,
        onOpen: (() -> Void)? = nil
    ) {
        let layout = DayTimelineLayoutCalculator(
            now: currentDate,
            hoursBefore: hoursBefore,
            hoursAfter: hoursAfter
        )
        self.segments     = segments
        self.currentDate  = currentDate
        self.timeFormat   = timeFormat
        self.hoursBefore  = hoursBefore
        self.hoursAfter   = hoursAfter
        self.onOpen       = onOpen
        self.layout       = layout
        self.eventRows    = layout.rows(for: segments)
        self.contentHeight = DayTimelineLayout.baseTrackHeight +
            DayTimelineLayout.rowHeight * CGFloat(max(eventRows.count - 1, 0))
    }

    // MARK: Body
    var body: some View {
        GeometryReader { proxy in
            let width = proxy.size.width

            ZStack(alignment: .topLeading) {

                // Hour grid
                ForEach(layout.hourTicks(), id: \.self) { tick in
                    let x = layout.xPosition(of: tick, width: width)
                    Path { path in
                        path.move(to: CGPoint(x: x, y: 0))
                        path.addLine(to: CGPoint(x: x, y: contentHeight))
                    }
                    .stroke(Color.primary.opacity(0.25)
, lineWidth: 1)

                    Text(hourFormatter.string(from: tick))
                        .font(.caption2)
                        .foregroundColor(.secondary)
                        .position(x: x, y: -8)
                }

                // Event rows
                ForEach(eventRows.indices, id: \.self) { row in
                    ForEach(eventRows[row]) { seg in
                        let startX  = layout.xPosition(of: max(seg.start, layout.visibleRange.lowerBound), width: width)
                        let endX    = layout.xPosition(of: min(seg.end, layout.visibleRange.upperBound), width: width)
                        let widthPx = max(endX - startX, DayTimelineLayout.segmentHeight / 2)

                        Capsule()
                            .fill(seg.color.opacity(seg.isHighlighted ? 0.55 : 0.25))
                            .overlay(
                                Capsule().stroke(
                                    seg.color,
                                    lineWidth: seg.isHighlighted ? 2 : 1
                                )
                            )
                            .frame(width: widthPx, height: DayTimelineLayout.segmentHeight)
                            .offset(
                                x: startX,
                                y: (DayTimelineLayout.baseTrackHeight - DayTimelineLayout.segmentHeight) / 2 +
                                   CGFloat(row) * DayTimelineLayout.rowHeight
                            )
                            .help(seg.title ?? "")
                    }
                }

                // Current time indicator
                if layout.visibleRange.contains(currentDate) {
                    let x = layout.xPosition(of: currentDate, width: width)
                    Rectangle()
                        .fill(Color.accentColor)
                        .frame(width: 2, height: contentHeight + 4)
                        .offset(x: x - 1, y: -2)
                    Circle()
                        .fill(Color.accentColor)
                        .frame(width: 6, height: 6)
                        .offset(x: x - 3, y: -5)
                }
            }
            .frame(height: contentHeight)
            .padding(.top, 10)
            .padding(.vertical, 8)
            .padding(.horizontal, 12)
        }
        .frame(maxWidth: .infinity)
        .contentShape(Rectangle())
        .onHover { hovering in
            guard onOpen != nil else { return }
            if hovering {
                NSCursor.pointingHand.push()
            } else {
                NSCursor.pop()
            }
        }
        .onTapGesture { onOpen?() }
        .accessibilityLabel("timeline_accessibility_label".loco(segments.count))
        .accessibilityAddTraits(onOpen == nil ? [] : .isButton)
    }

    private var hourFormatter: DateFormatter {
        let format = DateFormatter()
        format.locale = I18N.instance.locale
        switch timeFormat {
        case .am_pm:    format.dateFormat = "h a"
        case .military: format.dateFormat = "HH"
        }
        return format
    }
}

// MARK: — Preview

#Preview {
    let cal = Calendar.current
    let now = Date()
    let sampleSegments = [
        DaySegment(
            start: cal.date(byAdding: .hour, value: -2, to: now)!,
            end: cal.date(byAdding: .hour, value: -1, to: now)!,
            color: .blue
        ),
        DaySegment(
            start: cal.date(byAdding: .minute, value: -90, to: now)!,
            end: cal.date(byAdding: .minute, value: -30, to: now)!,
            color: .blue
        ),
        DaySegment(
            start: cal.date(byAdding: .minute, value: -90, to: now)!,
            end: cal.date(byAdding: .minute, value: 30, to: now)!,
            color: .blue
        ),
        DaySegment(
            start: cal.date(byAdding: .minute, value: -30, to: now)!,
            end: cal.date(byAdding: .minute, value: 30, to: now)!,
            color: .blue
        ),
        DaySegment(
            start: cal.date(byAdding: .hour, value: 1, to: now)!,
            end: cal.date(byAdding: .hour, value: 2, to: now)!,
            color: .green
        ),
        DaySegment(
            start: cal.date(byAdding: .hour, value: 4, to: now)!,
            end: cal.date(byAdding: .hour, value: 5, to: now)!,
            color: .orange
        )
    ]

    DayRelativeTimelineView(
        segments: sampleSegments,
        currentDate: now,
        timeFormat: .military
    )
    .padding()
}
